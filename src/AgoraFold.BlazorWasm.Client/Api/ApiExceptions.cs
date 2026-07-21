namespace AgoraFold.BlazorWasm.Client.Api;

/// <summary>
/// Base for HTTP-status-derived failures from the host's JSON API. Mirrors AgoraFold.Core's
/// NotFoundException/ForbiddenException/ValidationException split, translated from status codes
/// since the Client project doesn't reference Core (kept free of EF Core/Npgsql, not browser-wasm-safe).
/// </summary>
public class ApiException(string message) : Exception(message);

public sealed class ApiNotFoundException() : ApiException("The requested item was not found.");

public sealed class ApiForbiddenException() : ApiException("You do not have permission to do that.");

public sealed class ApiValidationException(IReadOnlyList<string> errors) : ApiException(string.Join(" ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
