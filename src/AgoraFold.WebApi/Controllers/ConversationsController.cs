using System.Security.Claims;
using AgoraFold.Core.Entities;
using AgoraFold.Core.Services;
using AgoraFold.WebApi.Filters;
using AgoraFold.WebApi.Messaging;
using AgoraFold.WebApi.Models.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.WebApi.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController(IConversationService conversationService, ConversationWebSocketManager webSocketManager) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryResponse>>> Inbox(CancellationToken cancellationToken)
    {
        var conversations = await conversationService.GetInboxAsync(CurrentUserId, cancellationToken);

        var result = conversations.Select(c =>
        {
            var lastMessage = c.Messages.FirstOrDefault();
            var otherPartyDisplayName = c.Listing.OwnerId == CurrentUserId
                ? c.Participant.DisplayName
                : c.Listing.Owner.DisplayName;

            return new ConversationSummaryResponse(
                c.Id,
                c.ListingId,
                c.Listing.Title,
                otherPartyDisplayName,
                lastMessage?.Body,
                lastMessage?.SentAt ?? c.StartedAt,
                lastMessage?.SenderId == CurrentUserId);
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConversationThreadResponse>> Details(int id, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetThreadAsync(id, CurrentUserId, cancellationToken);
        return Ok(ToThread(conversation));
    }

    [HttpPost]
    [ValidateCsrfToken]
    public async Task<ActionResult<ConversationThreadResponse>> Start(StartConversationRequest request, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.StartConversationAsync(request.ListingId, CurrentUserId, cancellationToken);
        var thread = await conversationService.GetThreadAsync(conversation.Id, CurrentUserId, cancellationToken);
        return CreatedAtAction(nameof(Details), new { id = conversation.Id }, ToThread(thread));
    }

    [HttpPost("{id:int}/replies")]
    [ValidateCsrfToken]
    public async Task<ActionResult<ConversationThreadResponse>> Reply(int id, ReplyRequest request, CancellationToken cancellationToken)
    {
        var message = await conversationService.PostReplyAsync(id, CurrentUserId, request.Body, cancellationToken);
        var conversation = await conversationService.GetThreadAsync(id, CurrentUserId, cancellationToken);

        var senderDisplayName = conversation.Messages.First(m => m.Id == message.Id).Sender.DisplayName;
        await webSocketManager.BroadcastMessageAsync(id, message, senderDisplayName);

        return Ok(ToThread(conversation));
    }

    private ConversationThreadResponse ToThread(Conversation conversation) =>
        new(
            conversation.Id,
            conversation.ListingId,
            conversation.Listing.Title,
            conversation.Messages.Select(m => new ConversationMessageResponse(
                m.Sender.DisplayName,
                m.Body,
                m.SentAt,
                m.SenderId == CurrentUserId)).ToList());
}
