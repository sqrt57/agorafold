using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgoraFold.BlazorWasm.Client.Api.Dto;

namespace AgoraFold.BlazorWasm.Client.Api;

/// <summary>
/// Thin JSON/multipart wrapper around the "Api" named HttpClient (same-origin, CsrfHandler
/// attached), analogous to AgoraFold.Vue's client.ts. Translates non-success responses into
/// ApiNotFoundException/ApiForbiddenException/ApiValidationException so pages can catch them the
/// same way BlazorServer's pages catch Core's NotFoundException/ForbiddenException/ValidationException.
/// </summary>
public sealed class ApiClient(IHttpClientFactory httpClientFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private HttpClient Http => httpClientFactory.CreateClient(ApiClientNames.Api);

    public async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using var response = await Http.GetAsync(url, cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken))!;
    }

    public async Task<T?> GetOrNullOnUnauthorizedAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using var response = await Http.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return default;
        }

        await ThrowIfErrorAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    public async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsJsonAsync(url, body, JsonOptions, cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken))!;
    }

    public async Task PostAsync(string url, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsync(url, content: null, cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);
    }

    public async Task<TResponse> PostFormAsync<TResponse>(string url, MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsync(url, content, cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken))!;
    }

    public async Task<TResponse> PutJsonAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PutAsJsonAsync(url, body, JsonOptions, cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken))!;
    }

    public async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        using var response = await Http.DeleteAsync(url, cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);
    }

    private static async Task ThrowIfErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                throw new ApiNotFoundException();
            case HttpStatusCode.Forbidden:
                throw new ApiForbiddenException();
            case HttpStatusCode.BadRequest:
                var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, cancellationToken);
                throw new ApiValidationException(error?.Errors ?? ["Request failed."]);
            default:
                throw new ApiException($"Request failed with status {(int)response.StatusCode}.");
        }
    }
}
