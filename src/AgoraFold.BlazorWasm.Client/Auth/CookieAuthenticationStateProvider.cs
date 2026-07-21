using System.Security.Claims;
using AgoraFold.BlazorWasm.Client.Api;
using AgoraFold.BlazorWasm.Client.Api.Dto.Account;
using Microsoft.AspNetCore.Components.Authorization;

namespace AgoraFold.BlazorWasm.Client.Auth;

/// <summary>
/// Backs the cascaded AuthenticationState from GET api/account/me (the auth cookie is HttpOnly, so
/// WASM can't read it directly). Login/Register/Logout call MarkAuthenticated/MarkLoggedOut directly
/// with the response they already have, instead of re-fetching /me, so AuthorizeView/[Authorize]
/// react immediately without an extra round-trip.
/// </summary>
public sealed class CookieAuthenticationStateProvider(AccountApiClient accountApiClient) : AuthenticationStateProvider
{
    private Task<AuthenticationState>? _cachedState;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        _cachedState ??= FetchAsync();
        return _cachedState;
    }

    public void MarkAuthenticated(UserResponse user)
    {
        _cachedState = Task.FromResult(BuildState(user));
        NotifyAuthenticationStateChanged(_cachedState);
    }

    public void MarkLoggedOut()
    {
        _cachedState = Task.FromResult(BuildState(user: null));
        NotifyAuthenticationStateChanged(_cachedState);
    }

    private async Task<AuthenticationState> FetchAsync()
    {
        var user = await accountApiClient.GetCurrentUserAsync();
        return BuildState(user);
    }

    private static AuthenticationState BuildState(UserResponse? user)
    {
        var identity = user is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.DisplayName),
                    new Claim(ClaimTypes.Email, user.Email),
                ],
                authenticationType: "ApiCookie");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
