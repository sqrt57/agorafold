namespace AgoraFold.BlazorWasm.Client.Api.Dto;

public sealed record ApiErrorResponse(IReadOnlyList<string> Errors);
