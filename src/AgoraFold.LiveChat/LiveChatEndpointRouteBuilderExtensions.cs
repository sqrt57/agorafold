using AgoraFold.LiveChat.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AgoraFold.LiveChat;

public static class LiveChatEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapConversationLiveChatEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/ws/conversations/{conversationId:int}") =>
        endpoints.MapGet(pattern, ConversationWebSocketEndpoint.HandleAsync);
}
