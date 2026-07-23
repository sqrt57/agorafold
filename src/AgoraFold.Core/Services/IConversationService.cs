using AgoraFold.Core.Entities;

namespace AgoraFold.Core.Services;

public interface IConversationService
{
    /// <summary>Idempotent per (listing, participant) pair — returns the existing conversation if one already exists.</summary>
    Task<Conversation> StartConversationAsync(int listingId, string participantId, CancellationToken cancellationToken = default);

    /// <summary>Full thread: ordered <see cref="Conversation.Messages"/> with senders loaded. Caller must be the listing owner or the participant.</summary>
    Task<Conversation> GetThreadAsync(int conversationId, string requestingUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a reply. When <paramref name="clientMessageId"/> is supplied the call is idempotent
    /// per (conversation, sender, key): a retry returns the already-persisted message instead of
    /// inserting a duplicate, so callers may safely resend after an ambiguous failure.
    /// </summary>
    Task<Message> PostReplyAsync(int conversationId, string senderId, string body, Guid? clientMessageId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// The signed-in user's conversations (as owner or participant), newest-activity first.
    /// Unlike <see cref="GetThreadAsync"/>, each conversation's <see cref="Conversation.Messages"/> contains at most its single latest message.
    /// </summary>
    Task<IReadOnlyList<Conversation>> GetInboxAsync(string userId, CancellationToken cancellationToken = default);
}
