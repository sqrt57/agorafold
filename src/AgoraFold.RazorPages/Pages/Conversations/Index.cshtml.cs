using AgoraFold.Core.Services;
using AgoraFold.RazorPages.Pages.Shared;

namespace AgoraFold.RazorPages.Pages.Conversations;

public class IndexModel(IConversationService conversationService) : AgoraFoldPageModel
{
    public IReadOnlyList<ConversationSummaryRow> Conversations { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var conversations = await conversationService.GetInboxAsync(CurrentUserId, cancellationToken);

        Conversations = conversations.Select(c =>
        {
            var lastMessage = c.Messages.FirstOrDefault();
            var otherPartyDisplayName = c.Listing.OwnerId == CurrentUserId
                ? c.Participant.DisplayName
                : c.Listing.Owner.DisplayName;

            return new ConversationSummaryRow(
                c.Id,
                c.ListingId,
                c.Listing.Title,
                otherPartyDisplayName,
                lastMessage?.Body,
                lastMessage?.SentAt ?? c.StartedAt,
                lastMessage?.SenderId == CurrentUserId);
        }).ToList();
    }
}
