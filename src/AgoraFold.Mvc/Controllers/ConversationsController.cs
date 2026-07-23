using System.Security.Claims;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Services;
using AgoraFold.Mvc.Models.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.Mvc.Controllers;

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
        return View(BuildThreadViewModel(conversation, new ConversationReplyViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, ConversationReplyViewModel reply, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await conversationService.PostReplyAsync(id, CurrentUserId, reply.Body, cancellationToken: cancellationToken);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ValidationException ex)
            {
                foreach (var error in ex.Errors)
                {
                    ModelState.AddModelError(nameof(reply.Body), error);
                }
            }
        }

        var conversation = await conversationService.GetThreadAsync(id, CurrentUserId, cancellationToken);
        return View("Details", BuildThreadViewModel(conversation, reply));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int listingId, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.StartConversationAsync(listingId, CurrentUserId, cancellationToken);
        return RedirectToAction(nameof(Details), new { id = conversation.Id });
    }

    private ConversationThreadViewModel BuildThreadViewModel(Core.Entities.Conversation conversation, ConversationReplyViewModel reply) =>
        new()
        {
            Id = conversation.Id,
            ListingId = conversation.ListingId,
            ListingTitle = conversation.Listing.Title,
            Messages = conversation.Messages.Select(m => new ConversationMessageViewModel(
                m.Sender.DisplayName,
                m.Body,
                m.SentAt,
                m.SenderId == CurrentUserId)).ToList(),
            Reply = reply,
        };
}
