namespace AgoraFold.Core.Entities;

public class Message
{
    public int Id { get; set; }
    public string Body { get; set; } = "";
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Client-supplied idempotency key. Retrying a send with the same key returns the
    /// already-persisted message instead of inserting a duplicate. Null for senders
    /// (e.g. server-rendered variants) that don't need retry-safe sends.
    /// </summary>
    public Guid? ClientMessageId { get; set; }

    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public string SenderId { get; set; } = "";
    public AppUser Sender { get; set; } = null!;
}
