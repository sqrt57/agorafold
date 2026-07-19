namespace AgoraFold.RazorPages.Pages.Conversations;

public record ConversationMessageRow(string SenderDisplayName, string Body, DateTime SentAt, bool IsMine);
