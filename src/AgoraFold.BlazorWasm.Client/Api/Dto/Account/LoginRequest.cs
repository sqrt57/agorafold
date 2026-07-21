namespace AgoraFold.BlazorWasm.Client.Api.Dto.Account;

public sealed record LoginRequest(string Email, string Password, bool RememberMe);
