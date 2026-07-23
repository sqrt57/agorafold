using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using AgoraFold.Core.Entities;
using AgoraFold.WebApi.Models.Conversations;

namespace AgoraFold.WebApi.Messaging;

public sealed class ConversationWebSocketManager
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, ConversationWebSocketConnection>> connections = new();

    // Serializes bucket membership changes. Without it, Remove can observe a bucket as
    // empty, race with an Add that inserts into that same bucket, and then remove the
    // bucket from the top-level map — orphaning the new connection: its client has seen
    // 'connected' but broadcasts can no longer find it, silently breaking the
    // "no missed messages after 'connected'" guarantee. Broadcasts stay lock-free.
    private readonly object membershipLock = new();

    public Task BroadcastMessageAsync(int conversationId, Message message, string senderDisplayName) =>
        BroadcastAsync(
            conversationId,
            ConversationWebSocketEvent.CreateMessage(ConversationWebSocketMessage.From(message, senderDisplayName)));

    public ConversationWebSocketConnection Add(int conversationId, WebSocket socket, CancellationToken requestAborted)
    {
        var connection = new ConversationWebSocketConnection(socket, requestAborted);
        lock (membershipLock)
        {
            var conversationConnections = connections.GetOrAdd(conversationId, _ => new());
            conversationConnections[connection.Id] = connection;
        }

        return connection;
    }

    public void Remove(int conversationId, ConversationWebSocketConnection connection)
    {
        lock (membershipLock)
        {
            if (!connections.TryGetValue(conversationId, out var conversationConnections))
            {
                return;
            }

            conversationConnections.TryRemove(connection.Id, out _);
            if (conversationConnections.IsEmpty)
            {
                connections.TryRemove(conversationId, out _);
            }
        }
    }

    public Task BroadcastAsync(int conversationId, ConversationWebSocketEvent message)
    {
        if (!connections.TryGetValue(conversationId, out var conversationConnections))
        {
            return Task.CompletedTask;
        }

        // Enqueue only — the actual socket writes happen on each connection's own pump, so
        // one stalled listener can neither delay the caller (the REST reply endpoint, or the
        // sending participant's receive loop) nor other participants' deliveries.
        foreach (var connection in conversationConnections.Values)
        {
            if (!connection.TryEnqueue(message))
            {
                Remove(conversationId, connection);
            }
        }

        return Task.CompletedTask;
    }
}

public sealed class ConversationWebSocketConnection : IAsyncDisposable
{
    /// <summary>
    /// Upper bound on undelivered broadcast events per connection. A consumer that falls this
    /// far behind is unhealthy — and silently dropping events would break the "no missed
    /// messages after 'connected'" guarantee — so overflow fails the connection instead and
    /// the client reconnects and reloads the thread snapshot.
    /// </summary>
    private const int OutboundQueueCapacity = 64;

    private static readonly TimeSpan DefaultSendTimeout = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly WebSocket socket;
    private readonly CancellationToken requestAborted;
    private readonly TimeSpan sendTimeout;
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private readonly Channel<ConversationWebSocketEvent> outbound =
        Channel.CreateBounded<ConversationWebSocketEvent>(new BoundedChannelOptions(OutboundQueueCapacity) { SingleReader = true });
    private readonly Task pumpTask;

    public ConversationWebSocketConnection(WebSocket socket, CancellationToken requestAborted, TimeSpan? sendTimeout = null)
    {
        this.socket = socket;
        this.requestAborted = requestAborted;
        this.sendTimeout = sendTimeout ?? DefaultSendTimeout;
        pumpTask = PumpOutboundAsync();
    }

    public Guid Id { get; } = Guid.NewGuid();
    public WebSocket Socket => socket;
    public WebSocketState SocketState => socket.State;

    public Task<WebSocketReceiveResult> ReceiveAsync(byte[] buffer, CancellationToken cancellationToken) =>
        socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

    /// <summary>
    /// Queues a broadcast event for this connection's outbound pump without blocking the
    /// caller. Returns false when the connection is unhealthy (queue overflowed or already
    /// failed); the connection is aborted rather than left subscribed with a missed event.
    /// </summary>
    public bool TryEnqueue(ConversationWebSocketEvent message)
    {
        if (outbound.Writer.TryWrite(message))
        {
            return true;
        }

        Fail();
        return false;
    }

    public async Task<bool> SendAsync(ConversationWebSocketEvent message)
    {
        if (socket.State != WebSocketState.Open || requestAborted.IsCancellationRequested)
        {
            return false;
        }

        // Bound the whole send (lock wait + socket write): a peer that stops reading fills
        // the TCP window and a socket write then hangs indefinitely.
        using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        sendCts.CancelAfter(sendTimeout);

        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
            await sendLock.WaitAsync(sendCts.Token);
            try
            {
                if (socket.State != WebSocketState.Open)
                {
                    return false;
                }

                await socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, sendCts.Token);
                return true;
            }
            finally
            {
                sendLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            if (!requestAborted.IsCancellationRequested)
            {
                // Timed out — the peer is unhealthy. Abort tears the socket down without any
                // further I/O, which ends its receive loop and triggers normal cleanup.
                Fail();
            }

            return false;
        }
        catch (WebSocketException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Sends a close frame under the send lock so it never interleaves with an in-flight
    /// <see cref="SendAsync"/>. Used to revoke connections whose session became invalid.
    /// Bounded like sends — a close handshake also writes to the socket.
    /// </summary>
    public async Task CloseAsync(WebSocketCloseStatus status, string description)
    {
        try
        {
            if (!await sendLock.WaitAsync(sendTimeout))
            {
                // An in-flight send is stalled; don't queue the close behind it.
                Fail();
                return;
            }

            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    using var closeCts = new CancellationTokenSource(sendTimeout);
                    await socket.CloseOutputAsync(status, description, closeCts.Token);
                }
            }
            finally
            {
                sendLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            Fail();
        }
        catch (WebSocketException)
        {
            // The peer may already have closed the connection.
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task PumpOutboundAsync()
    {
        try
        {
            await foreach (var message in outbound.Reader.ReadAllAsync(requestAborted))
            {
                if (!await SendAsync(message))
                {
                    Fail();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            // The endpoint's cleanup path awaits this task (via DisposeAsync); an unexpected
            // fault must tear the connection down, not propagate into that cleanup.
            Fail();
        }
    }

    private void Fail()
    {
        outbound.Writer.TryComplete();
        try
        {
            socket.Abort();
        }
        catch (ObjectDisposedException)
        {
            // Cleanup already disposed the socket; nothing left to tear down.
        }
    }

    public async ValueTask DisposeAsync()
    {
        outbound.Writer.TryComplete();
        await pumpTask; // bounded: an in-flight pump send times out rather than hanging
        await sendLock.WaitAsync();
        sendLock.Release();
        sendLock.Dispose();
    }
}
