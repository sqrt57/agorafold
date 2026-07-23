using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AgoraFold.Core.Entities;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Services;
using AgoraFold.WebApi.Models.Conversations;
using AgoraFold.WebApi.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AgoraFold.WebApi.Messaging;

public static class ConversationWebSocketEndpoint
{
    private const int MaxPayloadBytes = 16 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task HandleAsync(
        HttpContext context,
        int conversationId,
        IServiceScopeFactory scopeFactory,
        ConversationWebSocketManager manager,
        IOptionsMonitor<JsClientCorsOptions> corsOptions)
    {
        if (!IsAllowedOrigin(context, corsOptions.CurrentValue.JsClientOrigins))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("A WebSocket connection is required.");
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        try
        {
            await EnsureParticipantAsync(scopeFactory, conversationId, userId, context.RequestAborted);
        }
        catch (NotFoundException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        catch (ForbiddenException)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var connection = manager.Add(conversationId, socket, context.RequestAborted);

        try
        {
            await ReceiveMessagesAsync(context, conversationId, userId, connection, scopeFactory, manager);
        }
        finally
        {
            manager.Remove(conversationId, connection);
            await CloseSocketAsync(socket);
            await connection.DisposeAsync();
        }
    }

    private static async Task EnsureParticipantAsync(
        IServiceScopeFactory scopeFactory,
        int conversationId,
        string userId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
        await conversationService.GetThreadAsync(conversationId, userId, cancellationToken);
    }

    private static async Task ReceiveMessagesAsync(
        HttpContext context,
        int conversationId,
        string userId,
        ConversationWebSocketConnection connection,
        IServiceScopeFactory scopeFactory,
        ConversationWebSocketManager manager)
    {
        var buffer = new byte[4096];

        while (context.RequestAborted.IsCancellationRequested is false && connection.SocketState == WebSocketState.Open)
        {
            using var payload = new MemoryStream();
            WebSocketReceiveResult receiveResult;

            do
            {
                receiveResult = await connection.ReceiveAsync(buffer, context.RequestAborted);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                if (receiveResult.MessageType != WebSocketMessageType.Text)
                {
                    await connection.SendAsync(ConversationWebSocketEvent.CreateError("Only text messages are supported."));
                    return;
                }

                if (payload.Length + receiveResult.Count > MaxPayloadBytes)
                {
                    await connection.SendAsync(ConversationWebSocketEvent.CreateError("The message payload is too large."));
                    await CloseSocketAsync(connection.Socket);
                    return;
                }

                await payload.WriteAsync(buffer.AsMemory(0, receiveResult.Count), context.RequestAborted);
            }
            while (!receiveResult.EndOfMessage);

            await HandleMessageAsync(
                conversationId,
                userId,
                connection,
                scopeFactory,
                manager,
                Encoding.UTF8.GetString(payload.GetBuffer(), 0, checked((int)payload.Length)),
                context.RequestAborted);
        }
    }

    private static async Task HandleMessageAsync(
        int conversationId,
        string userId,
        ConversationWebSocketConnection connection,
        IServiceScopeFactory scopeFactory,
        ConversationWebSocketManager manager,
        string payload,
        CancellationToken cancellationToken)
    {
        ConversationWebSocketRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ConversationWebSocketRequest>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            await connection.SendAsync(ConversationWebSocketEvent.CreateError("The message must be valid JSON."));
            return;
        }

        if (request?.Type != "message")
        {
            await connection.SendAsync(ConversationWebSocketEvent.CreateError("The message type must be 'message'."));
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            var message = await conversationService.PostReplyAsync(conversationId, userId, request.Body ?? "", cancellationToken);
            var sender = await userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedAccessException("The signed-in user no longer exists.");

            await manager.BroadcastMessageAsync(conversationId, message, sender.DisplayName);
        }
        catch (ValidationException ex)
        {
            await connection.SendAsync(ConversationWebSocketEvent.CreateError(ex.Message));
        }
        catch (NotFoundException)
        {
            await connection.SendAsync(ConversationWebSocketEvent.CreateError("The conversation no longer exists."));
        }
        catch (ForbiddenException)
        {
            await connection.SendAsync(ConversationWebSocketEvent.CreateError("You are not a participant in this conversation."));
        }
        catch (UnauthorizedAccessException)
        {
            await connection.SendAsync(ConversationWebSocketEvent.CreateError("Your session is no longer valid."));
        }
    }

    private static bool IsAllowedOrigin(HttpContext context, IEnumerable<string> allowedOrigins)
    {
        var origin = context.Request.Headers.Origin.ToString();
        return string.IsNullOrEmpty(origin)
            || allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task CloseSocketAsync(WebSocket socket)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", CancellationToken.None);
            }
            catch (WebSocketException)
            {
                // The peer may already have closed the connection.
            }
        }
    }
}
