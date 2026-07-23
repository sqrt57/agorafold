using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using AgoraFold.Core.Entities;
using AgoraFold.WebApi.Models.Conversations;

namespace AgoraFold.WebApi.Messaging;

public sealed class ConversationWebSocketManager
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, ConversationWebSocketConnection>> connections = new();

    public Task BroadcastMessageAsync(int conversationId, Message message, string senderDisplayName) =>
        BroadcastAsync(
            conversationId,
            ConversationWebSocketEvent.CreateMessage(new ConversationWebSocketMessage(
                message.SenderId,
                senderDisplayName,
                message.Body,
                message.SentAt)));

    public ConversationWebSocketConnection Add(int conversationId, WebSocket socket, CancellationToken requestAborted)
    {
        var connection = new ConversationWebSocketConnection(socket, requestAborted);
        var conversationConnections = connections.GetOrAdd(conversationId, _ => new());
        conversationConnections[connection.Id] = connection;
        return connection;
    }

    public void Remove(int conversationId, ConversationWebSocketConnection connection)
    {
        if (!connections.TryGetValue(conversationId, out var conversationConnections))
        {
            return;
        }

        conversationConnections.TryRemove(connection.Id, out _);
        if (conversationConnections.IsEmpty)
        {
            connections.TryRemove(new KeyValuePair<int, ConcurrentDictionary<Guid, ConversationWebSocketConnection>>(conversationId, conversationConnections));
        }
    }

    public async Task BroadcastAsync(int conversationId, ConversationWebSocketEvent message)
    {
        if (!connections.TryGetValue(conversationId, out var conversationConnections))
        {
            return;
        }

        var sends = conversationConnections.Values.Select(async connection =>
        {
            if (!await connection.SendAsync(message))
            {
                Remove(conversationId, connection);
            }
        });

        await Task.WhenAll(sends);
    }
}

public sealed class ConversationWebSocketConnection(WebSocket socket, CancellationToken requestAborted)
    : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim sendLock = new(1, 1);

    public Guid Id { get; } = Guid.NewGuid();
    public WebSocket Socket => socket;
    public WebSocketState SocketState => socket.State;

    public Task<WebSocketReceiveResult> ReceiveAsync(byte[] buffer, CancellationToken cancellationToken) =>
        socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

    public async Task<bool> SendAsync(ConversationWebSocketEvent message)
    {
        if (socket.State != WebSocketState.Open || requestAborted.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
            await sendLock.WaitAsync(requestAborted);
            try
            {
                if (socket.State != WebSocketState.Open)
                {
                    return false;
                }

                await socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, requestAborted);
                return true;
            }
            finally
            {
                sendLock.Release();
            }
        }
        catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
        {
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

    public async ValueTask DisposeAsync()
    {
        await sendLock.WaitAsync();
        sendLock.Release();
        sendLock.Dispose();
    }
}
