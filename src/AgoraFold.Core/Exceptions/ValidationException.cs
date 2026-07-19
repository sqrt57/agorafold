namespace AgoraFold.Core.Exceptions;

public sealed class ValidationException : AgoraFoldException
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(string error) : this([error])
    {
    }

    public ValidationException(IReadOnlyList<string> errors) : base(string.Join(" ", errors))
    {
        Errors = errors;
    }
}
