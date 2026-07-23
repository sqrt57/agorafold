using Microsoft.AspNetCore.Http;

namespace AgoraFold.LiveChat.Origin;

/// <summary>
/// Same-origin mode for server-rendered hosts (MVC, Razor Pages, HTMX, Blazor WebAssembly) whose
/// browser client is served from the same origin as the WebSocket endpoint.
/// </summary>
public sealed class SameOriginPolicy : ILiveChatOriginPolicy
{
    public bool IsAllowed(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        return Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
            && string.Equals(originUri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase);
    }
}
