using AgoraFold.Core.Entities;

namespace AgoraFold.LiveChat;

public interface IConversationEventPublisher
{
    Task PublishMessageAsync(int conversationId, Message message, string senderDisplayName);
}
