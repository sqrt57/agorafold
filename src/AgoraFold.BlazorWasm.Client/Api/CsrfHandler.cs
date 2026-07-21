using System.Net.Http.Json;
using System.Text.Json;

namespace AgoraFold.BlazorWasm.Client.Api;

/// <summary>
/// Attaches a fresh X-CSRF-TOKEN header to every mutating request, mirroring AgoraFold.Vue's
/// client.ts. The token is bound to whichever identity was active when it was issued, so it's
/// fetched immediately before each mutating call rather than cached - a token cached from before
/// login/logout would be rejected once the identity changes.
/// </summary>
public sealed class CsrfHandler(IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    private static readonly HashSet<HttpMethod> MutatingMethods =
        [HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Patch];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (MutatingMethods.Contains(request.Method))
        {
            // Uses the undecorated "ApiRaw" client so this fetch doesn't loop back through this
            // same handler.
            using var tokenClient = httpClientFactory.CreateClient(ApiClientNames.Raw);
            var token = await tokenClient.GetFromJsonAsync<CsrfTokenResponse>("api/antiforgery/token", JsonOptions, cancellationToken);
            request.Headers.Add("X-CSRF-TOKEN", token!.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private sealed record CsrfTokenResponse(string Token);
}
