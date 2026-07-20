namespace AgoraFold.WebApi.Models.Account;

public sealed record LoginRequest(string Email, string Password, bool RememberMe);
