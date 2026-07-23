using AgoraFold.Core.Entities;

namespace AgoraFold.LiveChat.Protocol;

public sealed record ConversationWebSocketRequest(string? Type, string? Body, Guid? ClientMessageId);

public sealed record ConversationWebSocketEvent(
    string Type,
    ConversationWebSocketMessage? Message,
    string? Error,
    Guid? ClientMessageId)
{
    /// <summary>Sent once the connection is registered for broadcasts — the client's cue that a thread snapshot fetched from now on cannot miss live messages.</summary>
    public static ConversationWebSocketEvent CreateConnected() =>
        new("connected", null, null, null);

    public static ConversationWebSocketEvent CreateMessage(ConversationWebSocketMessage message) =>
        new("message", message, null, null);

    /// <summary>Sent only to the connection that submitted the send, confirming persistence of its <paramref name="clientMessageId"/>.</summary>
    public static ConversationWebSocketEvent CreateAck(Guid clientMessageId, ConversationWebSocketMessage message) =>
        new("ack", message, null, clientMessageId);

    public static ConversationWebSocketEvent CreateError(string error, Guid? clientMessageId = null) =>
        new("error", null, error, clientMessageId);
}

public sealed record ConversationWebSocketMessage(
    int Id,
    string SenderId,
    string SenderDisplayName,
    string Body,
    DateTime SentAt)
{
    public static ConversationWebSocketMessage From(Message message, string senderDisplayName) =>
        new(message.Id, message.SenderId, senderDisplayName, message.Body, message.SentAt);
}
