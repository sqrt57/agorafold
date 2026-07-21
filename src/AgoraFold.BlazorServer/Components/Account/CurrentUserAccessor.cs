using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace AgoraFold.BlazorServer.Account;

// Mirrors Mvc's `CurrentUserId` controller property - components can't read HttpContext.User
// synchronously, so this wraps the async AuthenticationStateProvider dance once per page.
public sealed class CurrentUserAccessor(AuthenticationStateProvider authenticationStateProvider)
{
    public async Task<string?> GetUserIdAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
