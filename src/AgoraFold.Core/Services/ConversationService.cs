using AgoraFold.Core.Entities;
using AgoraFold.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AgoraFold.Core.Services;

public sealed class ConversationService(AppDbContext db) : IConversationService
{
    private const int MaxBodyLength = 4000;

    public async Task<Conversation> StartConversationAsync(int listingId, string participantId, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), listingId);

        if (listing.OwnerId == participantId)
        {
            throw new ForbiddenException("The listing owner cannot start a conversation with themselves.");
        }

        var existing = await db.Conversations
            .Include(c => c.Listing)
            .FirstOrDefaultAsync(c => c.ListingId == listingId && c.ParticipantId == participantId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var conversation = new Conversation
        {
            ListingId = listingId,
            Listing = listing,
            ParticipantId = participantId,
            StartedAt = DateTime.UtcNow,
        };

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);

        return conversation;
    }

    public async Task<Conversation> GetThreadAsync(int conversationId, string requestingUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await db.Conversations
            .Include(c => c.Listing)
            .Include(c => c.Participant)
            .Include(c => c.Messages.OrderBy(m => m.SentAt).ThenBy(m => m.Id))
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Conversation), conversationId);

        EnsureParticipant(conversation, requestingUserId);

        // The Include's OrderBy only guarantees SQL order for rows freshly materialized by
        // this query. When the same DbContext already tracks a just-added Message (e.g. this
        // call follows PostReplyAsync in the same request), EF's identity map keeps that
        // instance at its existing position in the collection instead of the queried order,
        // so re-sort explicitly to guarantee callers see ascending SentAt order regardless.
        // Id breaks SentAt ties deterministically (insertion order).
        conversation.Messages = conversation.Messages.OrderBy(m => m.SentAt).ThenBy(m => m.Id).ToList();

        return conversation;
    }

    public async Task<Message> PostReplyAsync(int conversationId, string senderId, string body, Guid? clientMessageId = null, CancellationToken cancellationToken = default)
    {
        var conversation = await db.Conversations
            .Include(c => c.Listing)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Conversation), conversationId);

        EnsureParticipant(conversation, senderId);

        if (clientMessageId is not null)
        {
            var existing = await FindByClientMessageIdAsync(conversationId, senderId, clientMessageId.Value, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ValidationException("Message body is required.");
        }

        var trimmed = body.Trim();
        if (trimmed.Length > MaxBodyLength)
        {
            throw new ValidationException($"Message body must be {MaxBodyLength} characters or fewer.");
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Body = trimmed,
            SentAt = DateTime.UtcNow,
            ClientMessageId = clientMessageId,
        };

        db.Messages.Add(message);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (clientMessageId is not null)
        {
            // A concurrent retry of the same send won the unique (conversation, sender,
            // client_message_id) index race. Detach the losing entity so it can't leak into
            // later queries via change-tracker fixup, and return the persisted winner.
            db.Entry(message).State = EntityState.Detached;
            var existing = await FindByClientMessageIdAsync(conversationId, senderId, clientMessageId.Value, cancellationToken);
            if (existing is null)
            {
                throw;
            }

            return existing;
        }

        return message;
    }

    private Task<Message?> FindByClientMessageIdAsync(int conversationId, string senderId, Guid clientMessageId, CancellationToken cancellationToken) =>
        db.Messages.FirstOrDefaultAsync(
            m => m.ConversationId == conversationId && m.SenderId == senderId && m.ClientMessageId == clientMessageId,
            cancellationToken);

    public async Task<IReadOnlyList<Conversation>> GetInboxAsync(string userId, CancellationToken cancellationToken = default)
    {
        var conversations = await db.Conversations.AsNoTracking()
            .Where(c => c.Listing.OwnerId == userId || c.ParticipantId == userId)
            .Include(c => c.Listing)
                .ThenInclude(l => l.Owner)
            .Include(c => c.Participant)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .ToListAsync(cancellationToken);

        return conversations
            .OrderByDescending(c => c.Messages.Count > 0 ? c.Messages.First().SentAt : c.StartedAt)
            .ToList();
    }

    private static void EnsureParticipant(Conversation conversation, string userId)
    {
        if (conversation.Listing.OwnerId != userId && conversation.ParticipantId != userId)
        {
            throw new ForbiddenException("You are not a participant in this conversation.");
        }
    }
}
