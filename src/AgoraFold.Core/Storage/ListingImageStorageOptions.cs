namespace AgoraFold.Core.Storage;

public sealed class ListingImageStorageOptions
{
    /// <summary>Absolute filesystem directory listing images are stored under. No default — each variant must set it.</summary>
    public string RootPath { get; set; } = "";
}
