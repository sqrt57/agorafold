using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace AgoraFold.BlazorWasm.Client.Auth;

// Mirrors AgoraFold.BlazorServer's CurrentUserAccessor - wraps the async AuthenticationStateProvider
// dance once per page. Render-mode agnostic, so it ports over unchanged aside from the namespace.
public sealed class CurrentUserAccessor(AuthenticationStateProvider authenticationStateProvider)
{
    public async Task<string?> GetUserIdAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        // Plain ClaimsPrincipal.FindFirst rather than the FindFirstValue extension: that extension
        // lives in an ASP.NET Core hosting package not meant for the browser-wasm target.
        return state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
