namespace AgoraFold.Core.Entities;

public class Message
{
    public int Id { get; set; }
    public string Body { get; set; } = "";
    public DateTime SentAt { get; set; }

    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public string SenderId { get; set; } = "";
    public AppUser Sender { get; set; } = null!;
}
