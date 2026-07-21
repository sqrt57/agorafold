using AgoraFold.BlazorWasm.Client.Api.Dto.Account;

namespace AgoraFold.BlazorWasm.Client.Api;

public sealed class AccountApiClient(ApiClient api)
{
    public Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        api.PostJsonAsync<RegisterRequest, UserResponse>("api/account/register", request, cancellationToken);

    public Task<UserResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        api.PostJsonAsync<LoginRequest, UserResponse>("api/account/login", request, cancellationToken);

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        api.PostAsync("api/account/logout", cancellationToken);

    public Task<UserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
        api.GetOrNullOnUnauthorizedAsync<UserResponse>("api/account/me", cancellationToken);
}
