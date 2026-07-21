using AgoraFold.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.BlazorServer.Account;

public static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // Signing out writes a Set-Cookie header, which needs a real HTTP response - not available
    // from inside an interactive circuit - so this stays a plain minimal-API endpoint rather
    // than a routable component.
    public static IEndpointRouteBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/Account");

        group.MapPost("/Logout", async (SignInManager<AppUser> signInManager, [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect(returnUrl);
        });

        return endpoints;
    }
}
