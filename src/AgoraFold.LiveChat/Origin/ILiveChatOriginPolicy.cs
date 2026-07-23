using Microsoft.AspNetCore.Http;

namespace AgoraFold.LiveChat.Origin;

public interface ILiveChatOriginPolicy
{
    bool IsAllowed(HttpContext context);
}
