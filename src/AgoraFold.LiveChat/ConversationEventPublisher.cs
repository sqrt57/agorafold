using AgoraFold.Core.Entities;
using AgoraFold.LiveChat.Transport;

namespace AgoraFold.LiveChat;

public sealed class ConversationEventPublisher(ConversationWebSocketManager manager) : IConversationEventPublisher
{
    public Task PublishMessageAsync(int conversationId, Message message, string senderDisplayName) =>
        manager.BroadcastMessageAsync(conversationId, message, senderDisplayName);
}
