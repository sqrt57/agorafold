namespace AgoraFold.BlazorWasm.Client.Api;

/// <summary>An image selected via InputFile, ready to attach to a multipart request.</summary>
public sealed record ListingFileUpload(string FileName, string ContentType, Stream Content);
