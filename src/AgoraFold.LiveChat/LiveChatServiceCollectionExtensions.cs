using AgoraFold.LiveChat.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace AgoraFold.LiveChat;

public static class LiveChatServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared connection manager and publisher. Callers must separately register an
    /// <see cref="Origin.ILiveChatOriginPolicy"/> appropriate to their hosting model before mapping
    /// the endpoint (see <see cref="LiveChatEndpointRouteBuilderExtensions"/>).
    /// </summary>
    public static IServiceCollection AddLiveChat(this IServiceCollection services)
    {
        services.AddSingleton<ConversationWebSocketManager>();
        services.AddSingleton<IConversationEventPublisher, ConversationEventPublisher>();
        return services;
    }
}
