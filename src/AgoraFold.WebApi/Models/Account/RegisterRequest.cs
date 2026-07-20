namespace AgoraFold.WebApi.Models.Account;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);
