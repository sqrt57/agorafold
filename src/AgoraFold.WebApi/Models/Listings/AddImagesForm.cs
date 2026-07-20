using Microsoft.AspNetCore.Http;

namespace AgoraFold.WebApi.Models.Listings;

public sealed class AddImagesForm
{
    public List<IFormFile> Images { get; set; } = [];
}
