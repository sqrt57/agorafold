using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace AgoraFold.BlazorServer.Account;

internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    [DoesNotReturn]
    public void RedirectTo(string uri)
    {
        navigationManager.NavigateTo(uri, forceLoad: true);
        throw new InvalidOperationException($"{nameof(IdentityRedirectManager)} can only be used during static rendering.");
    }
}
