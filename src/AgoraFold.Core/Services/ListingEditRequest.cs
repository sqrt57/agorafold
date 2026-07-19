namespace AgoraFold.Core.Services;

public sealed record ListingEditRequest(string Title, string Description, decimal? Price, int CategoryId);
