namespace AgoraFold.BlazorWasm.Models.Listings;

// IFormFile is a server-only ASP.NET Core type, so multipart form-binding models live here in the
// host rather than the Client project's DTOs (which stay reference-free of anything server-only).
public sealed class ListingCreateForm
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? Price { get; set; }
    public int CategoryId { get; set; }
    public List<IFormFile>? Images { get; set; }
}
