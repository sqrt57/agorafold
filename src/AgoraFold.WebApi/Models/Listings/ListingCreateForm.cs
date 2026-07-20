using Microsoft.AspNetCore.Http;

namespace AgoraFold.WebApi.Models.Listings;

public sealed class ListingCreateForm
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? Price { get; set; }
    public int CategoryId { get; set; }
    public List<IFormFile>? Images { get; set; }
}
