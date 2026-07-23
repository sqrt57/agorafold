namespace AgoraFold.WebApi.Models.Conversations;

/// <summary>ClientMessageId is an optional idempotency key — retrying a send with the same key returns the already-persisted message instead of duplicating it.</summary>
public sealed record ReplyRequest(string Body, Guid? ClientMessageId = null);
