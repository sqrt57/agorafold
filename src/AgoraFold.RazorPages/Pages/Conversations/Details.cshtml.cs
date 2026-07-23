using System.ComponentModel.DataAnnotations;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Services;
using AgoraFold.RazorPages.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.RazorPages.Pages.Conversations;

public class DetailsModel(IConversationService conversationService) : AgoraFoldPageModel
{
    public int Id { get; private set; }

    public int ListingId { get; private set; }

    public string ListingTitle { get; private set; } = "";

    public IReadOnlyList<ConversationMessageRow> Messages { get; private set; } = [];

    [BindProperty]
    [Required]
    [StringLength(4000)]
    public string ReplyBody { get; set; } = "";

    public async Task OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetThreadAsync(id, CurrentUserId, cancellationToken);
        LoadFrom(conversation);
    }

    public async Task<IActionResult> OnPostReplyAsync(int id, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await conversationService.PostReplyAsync(id, CurrentUserId, ReplyBody, cancellationToken: cancellationToken);
                return RedirectToPage(new { id });
            }
            catch (AgoraFold.Core.Exceptions.ValidationException ex)
            {
                foreach (var error in ex.Errors)
                {
                    ModelState.AddModelError(nameof(ReplyBody), error);
                }
            }
        }

        // Intentionally not a redirect: re-render this page in place so the just-typed
        // reply text (still bound in ReplyBody) and its validation error aren't lost.
        var conversation = await conversationService.GetThreadAsync(id, CurrentUserId, cancellationToken);
        LoadFrom(conversation);
        return Page();
    }

    private void LoadFrom(Core.Entities.Conversation conversation)
    {
        Id = conversation.Id;
        ListingId = conversation.ListingId;
        ListingTitle = conversation.Listing.Title;
        Messages = conversation.Messages.Select(m => new ConversationMessageRow(
            m.Sender.DisplayName,
            m.Body,
            m.SentAt,
            m.SenderId == CurrentUserId)).ToList();
    }
}
