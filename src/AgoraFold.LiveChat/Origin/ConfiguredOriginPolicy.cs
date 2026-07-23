using Microsoft.AspNetCore.Http;

namespace AgoraFold.LiveChat.Origin;

/// <summary>
/// Allowlist mode for hosts serving JavaScript clients from a different origin (e.g. a Vite dev
/// server). The allowed-origins list is supplied as a delegate so this project stays ignorant of
/// any particular host's concrete options type.
/// </summary>
public sealed class ConfiguredOriginPolicy(Func<IEnumerable<string>> allowedOrigins) : ILiveChatOriginPolicy
{
    public bool IsAllowed(HttpContext context)
    {
        // An absent Origin is allowed deliberately: browsers always send Origin on WebSocket
        // handshakes, so cross-site hijacking is still blocked by the allowlist — only
        // non-browser tooling omits the header, and it still needs the auth cookie.
        var origin = context.Request.Headers.Origin.ToString();
        return string.IsNullOrEmpty(origin)
            || allowedOrigins().Contains(origin, StringComparer.OrdinalIgnoreCase);
    }
}
