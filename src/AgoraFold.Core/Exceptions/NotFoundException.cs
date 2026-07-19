namespace AgoraFold.Core.Exceptions;

public sealed class NotFoundException(string resourceName, object key)
    : AgoraFoldException($"{resourceName} '{key}' was not found.")
{
    public string ResourceName { get; } = resourceName;
    public object Key { get; } = key;
}
