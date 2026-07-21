namespace AgoraFold.WebApi.Models.Conversations;

public sealed record ConversationWebSocketRequest(string? Type, string? Body);

public sealed record ConversationWebSocketEvent(
    string Type,
    ConversationWebSocketMessage? Message,
    string? Error)
{
    public static ConversationWebSocketEvent CreateMessage(ConversationWebSocketMessage message) =>
        new("message", message, null);

    public static ConversationWebSocketEvent CreateError(string error) =>
        new("error", null, error);
}

public sealed record ConversationWebSocketMessage(
    string SenderId,
    string SenderDisplayName,
    string Body,
    DateTime SentAt);
