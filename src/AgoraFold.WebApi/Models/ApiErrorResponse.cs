namespace AgoraFold.WebApi.Models;

public sealed record ApiErrorResponse(IReadOnlyList<string> Errors);
