using System.Net.WebSockets;
using AgoraFold.WebApi.Messaging;
using AgoraFold.WebApi.Models.Conversations;

namespace AgoraFold.WebApi.Tests;

public sealed class ConversationWebSocketManagerTests
{
    private static ConversationWebSocketEvent MessageEvent(int id = 1) =>
        ConversationWebSocketEvent.CreateMessage(
            new ConversationWebSocketMessage(id, "sender-1", "Sender", "hello", DateTime.UtcNow));

    [Fact]
    public async Task BroadcastDoesNotWaitForAStalledPeer()
    {
        var manager = new ConversationWebSocketManager();
        var stalledSocket = new StubWebSocket { BlockSends = true };
        var healthySocket = new StubWebSocket();
        var stalled = manager.Add(1, stalledSocket, CancellationToken.None);
        var healthy = manager.Add(1, healthySocket, CancellationToken.None);

        // The broadcast must complete promptly even though the stalled peer's socket write
        // never finishes — this is what keeps the REST reply endpoint responsive.
        var broadcast = manager.BroadcastAsync(1, MessageEvent());
        Assert.Same(broadcast, await Task.WhenAny(broadcast, Task.Delay(TimeSpan.FromSeconds(2))));

        // The healthy peer still receives the event via its own pump.
        await healthySocket.WaitForFirstSendAsync(TimeSpan.FromSeconds(5));

        stalledSocket.Abort();
        await stalled.DisposeAsync();
        await healthy.DisposeAsync();
    }

    [Fact]
    public async Task SendTimesOutAndAbortsTheConnectionWhenThePeerStopsReading()
    {
        var socket = new StubWebSocket { BlockSends = true };
        await using var connection = new ConversationWebSocketConnection(
            socket, CancellationToken.None, sendTimeout: TimeSpan.FromMilliseconds(200));

        var sent = await connection.SendAsync(MessageEvent());

        Assert.False(sent);
        Assert.True(socket.Aborted);
    }

    [Fact]
    public async Task OverflowingTheOutboundQueueFailsTheConnection()
    {
        var socket = new StubWebSocket { BlockSends = true };
        // Long send timeout so the pump stays stuck in its first send while the queue fills.
        var connection = new ConversationWebSocketConnection(
            socket, CancellationToken.None, sendTimeout: TimeSpan.FromSeconds(30));

        var accepted = 0;
        while (accepted < 500 && connection.TryEnqueue(MessageEvent(accepted)))
        {
            accepted++;
        }

        // The bounded queue rejects the overflow and the connection is failed (aborted) so
        // its client reconnects instead of staying subscribed with missed events.
        Assert.True(accepted < 500);
        Assert.True(socket.Aborted);
        Assert.False(connection.TryEnqueue(MessageEvent()));

        await connection.DisposeAsync();
    }
}

/// <summary>
/// A WebSocket whose sends either complete immediately or, with <see cref="BlockSends"/>,
/// hang until cancelled or aborted — simulating a peer that stops reading so the TCP
/// window fills and socket writes never finish.
/// </summary>
internal sealed class StubWebSocket : WebSocket
{
    private readonly object gate = new();
    private readonly List<TaskCompletionSource> pendingSends = [];
    private readonly TaskCompletionSource firstSend = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private WebSocketState state = WebSocketState.Open;

    public bool BlockSends { get; init; }
    public bool Aborted { get; private set; }

    public override WebSocketCloseStatus? CloseStatus => null;
    public override string? CloseStatusDescription => null;
    public override WebSocketState State => state;
    public override string? SubProtocol => null;

    public Task WaitForFirstSendAsync(TimeSpan timeout) => firstSend.Task.WaitAsync(timeout);

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        firstSend.TrySetResult();

        if (!BlockSends)
        {
            return Task.CompletedTask;
        }

        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gate)
        {
            if (Aborted)
            {
                return Task.FromException(new WebSocketException(WebSocketError.ConnectionClosedPrematurely));
            }

            pendingSends.Add(pending);
        }

        cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
        return pending.Task;
    }

    public override void Abort()
    {
        lock (gate)
        {
            Aborted = true;
            state = WebSocketState.Aborted;
            foreach (var pending in pendingSends)
            {
                pending.TrySetException(new WebSocketException(WebSocketError.ConnectionClosedPrematurely));
            }

            pendingSends.Clear();
        }
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
        new TaskCompletionSource<WebSocketReceiveResult>().Task;

    public override void Dispose()
    {
    }
}
