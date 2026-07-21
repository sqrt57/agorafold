using System.Security.Claims;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Services;
using AgoraFold.Htmx.Models.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.Htmx.Controllers;

[Authorize]
public class ConversationsController(IConversationService conversationService) : Controller
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var conversations = await conversationService.GetInboxAsync(CurrentUserId, cancellationToken);

        var vm = conversations.Select(c =>
        {
            var lastMessage = c.Messages.FirstOrDefault();
            var otherPartyDisplayName = c.Listing.OwnerId == CurrentUserId
                ? c.Participant.DisplayName
                : c.Listing.Owner.DisplayName;

            return new ConversationSummaryViewModel(
                c.Id,
                c.ListingId,
                c.Listing.Title,
                otherPartyDisplayName,
                lastMessage?.Body,
                lastMessage?.SentAt ?? c.StartedAt,
                lastMessage?.SenderId == CurrentUserId);
        }).ToList();

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetThreadAsync(id, CurrentUserId, cancellationToken);
        return View(BuildThreadViewModel(conversation));
    }

    /// <summary>
    /// Polled every 5s by the thread view (`hx-trigger="every 5s"`) while a conversation is open. Only
    /// returns messages newer than <paramref name="sinceId"/> instead of the whole thread, keeping each
    /// poll response small - see the `#last-message-id` marker this and <see cref="Reply"/> both update
    /// out-of-band (`hx-swap-oob`) so the next poll knows where to resume from.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Poll(int id, int sinceId, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetThreadAsync(id, CurrentUserId, cancellationToken);

        var newMessages = conversation.Messages
            .Where(m => m.Id > sinceId)
            .Select(ToMessageViewModel)
            .ToList();

        return PartialView("_Messages", newMessages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, ConversationReplyViewModel reply, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await conversationService.PostReplyAsync(id, CurrentUserId, reply.Body, cancellationToken);
                var conversation = await conversationService.GetThreadAsync(id, CurrentUserId, cancellationToken);
                var newMessage = conversation.Messages
                    .OrderByDescending(m => m.Id)
                    .Select(ToMessageViewModel)
                    .First();

                return PartialView("_Messages", new List<ConversationMessageViewModel> { newMessage });
            }
            catch (ValidationException ex)
            {
                foreach (var error in ex.Errors)
                {
                    ModelState.AddModelError(nameof(reply.Body), error);
                }
            }
        }

        // The reply form's default hx-target/hx-swap append a successful new message to #messages;
        // on failure retarget/reswap this one response into the inline error slot instead.
        Response.Headers["HX-Retarget"] = "#reply-error";
        Response.Headers["HX-Reswap"] = "innerHTML";
        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
        return PartialView("_ReplyError", errors);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int listingId, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.StartConversationAsync(listingId, CurrentUserId, cancellationToken);
        return RedirectToAction(nameof(Details), new { id = conversation.Id });
    }

    private ConversationMessageViewModel ToMessageViewModel(Core.Entities.Message m) =>
        new(m.Id, m.Sender.DisplayName, m.Body, m.SentAt, m.SenderId == CurrentUserId);

    private ConversationThreadViewModel BuildThreadViewModel(Core.Entities.Conversation conversation) =>
        new()
        {
            Id = conversation.Id,
            ListingId = conversation.ListingId,
            ListingTitle = conversation.Listing.Title,
            Messages = conversation.Messages.Select(ToMessageViewModel).ToList(),
        };
}
