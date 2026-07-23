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
    private const string SessionInvalidError = "Your session is no longer valid.";

    // Cookie authentication only re-checks the security stamp on HTTP requests (and only every
    // SecurityStampValidationInterval), which never covers an established socket — so the socket
    // re-checks it itself: at the handshake, on every incoming message, and on this timer for
    // idle listeners. Keeps deactivated/deleted accounts and rotated stamps (password change,
    // admin deactivation) from holding a live subscription.
    private static readonly TimeSpan SessionRevalidationInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(10);

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
        if (userId is null || await ValidateSessionAsync(scopeFactory, context.User) is null)
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

        using var revalidationCts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        var revalidationTask = RevalidateSessionPeriodicallyAsync(context.User, connection, scopeFactory, revalidationCts.Token);

        try
        {
            // Sent only after manager.Add: once the client observes this event, every later
            // commit is guaranteed to reach it as a broadcast, so a thread snapshot fetched
            // from this point on (merged by message id) cannot miss messages.
            await connection.SendAsync(ConversationWebSocketEvent.CreateConnected());

            await ReceiveMessagesAsync(context, conversationId, userId, connection, scopeFactory, manager);
        }
        finally
        {
            revalidationCts.Cancel();
            try
            {
                await revalidationTask;
            }
            catch (OperationCanceledException)
            {
            }

            manager.Remove(conversationId, connection);
            // Dispose before closing: DisposeAsync completes the outbound queue and awaits
            // the pump, so already-enqueued broadcasts flush while the socket is still Open.
            // Closing first would flip the socket out of Open mid-drain, making the pump's
            // remaining sends fail and abort the socket right after the graceful close.
            await connection.DisposeAsync();
            await CloseSocketAsync(socket);
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

    /// <summary>The signed-in user when the principal's security stamp is still current and the account is not locked out (deactivated); otherwise null.</summary>
    private static async Task<AppUser?> ValidateSessionAsync(IServiceScopeFactory scopeFactory, ClaimsPrincipal principal)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<AppUser>>();

        var user = await signInManager.ValidateSecurityStampAsync(principal);
        if (user is null)
        {
            return null;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        return await userManager.IsLockedOutAsync(user) ? null : user;
    }

    private static async Task RevalidateSessionPeriodicallyAsync(
        ClaimsPrincipal principal,
        ConversationWebSocketConnection connection,
        IServiceScopeFactory scopeFactory,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && connection.SocketState == WebSocketState.Open)
        {
            await Task.Delay(SessionRevalidationInterval, cancellationToken);

            if (await ValidateSessionAsync(scopeFactory, principal) is null)
            {
                await connection.SendAsync(ConversationWebSocketEvent.CreateError(SessionInvalidError));
                await connection.CloseAsync(WebSocketCloseStatus.PolicyViolation, SessionInvalidError);
                return;
            }
        }
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
                    // Close through the connection so the close frame serializes under the
                    // send lock instead of interleaving with an in-flight pump send.
                    await connection.CloseAsync(WebSocketCloseStatus.MessageTooBig, "The message payload is too large.");
                    return;
                }

                await payload.WriteAsync(buffer.AsMemory(0, receiveResult.Count), context.RequestAborted);
            }
            while (!receiveResult.EndOfMessage);

            await HandleMessageAsync(
                context,
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
        HttpContext context,
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

        var clientMessageId = request.ClientMessageId;
        try
        {
            var sender = await ValidateSessionAsync(scopeFactory, context.User);
            if (sender is null)
            {
                await connection.SendAsync(ConversationWebSocketEvent.CreateError(SessionInvalidError, clientMessageId));
                await connection.CloseAsync(WebSocketCloseStatus.PolicyViolation, SessionInvalidError);
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();

            var message = await conversationService.PostReplyAsync(conversationId, userId, request.Body ?? "", clientMessageId, cancellationToken);
            var webSocketMessage = ConversationWebSocketMessage.From(message, sender.DisplayName);

            // Ack the sending connection first so its draft clears promptly, then broadcast to
            // every participant (including the sender — clients dedupe by message id).
            if (clientMessageId is not null)
            {
                await connection.SendAsync(ConversationWebSocketEvent.CreateAck(clientMessageId.Value, webSocketMessage));
            }

            await manager.BroadcastAsync(conversationId, ConversationWebSocketEvent.CreateMessage(webSocketMessage));
        }
        catch (ValidationException ex)
        {
            await connection.SendAsync(ConversationWebSocketEvent.CreateError(ex.Message, clientMessageId));
        }
        catch (NotFoundException)
        {
            await connection.SendAsync(ConversationWebSocketEvent.CreateError("The conversation no longer exists.", clientMessageId));
        }
        catch (ForbiddenException)
        {
            await connection.SendAsync(ConversationWebSocketEvent.CreateError("You are not a participant in this conversation.", clientMessageId));
        }
    }

    private static bool IsAllowedOrigin(HttpContext context, IEnumerable<string> allowedOrigins)
    {
        // An absent Origin is allowed deliberately: browsers always send Origin on WebSocket
        // handshakes, so cross-site hijacking is still blocked by the allowlist — only
        // non-browser tooling omits the header, and it still needs the auth cookie.
        var origin = context.Request.Headers.Origin.ToString();
        return string.IsNullOrEmpty(origin)
            || allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    // Internal (not private) so the WebApi.Tests project can drive the exact cleanup call the
    // endpoint's finally block makes, rather than a re-implementation that could drift from it.
    internal static async Task CloseSocketAsync(WebSocket socket)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                // Bounded: the graceful close handshake writes to the socket and waits for
                // the peer's close frame — a stalled peer must not pin the request here.
                using var closeCts = new CancellationTokenSource(CloseTimeout);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", closeCts.Token);
            }
            catch (OperationCanceledException)
            {
                socket.Abort();
            }
            catch (WebSocketException)
            {
                // The peer may already have closed the connection.
            }
        }
    }
}
