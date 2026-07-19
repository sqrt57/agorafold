using Microsoft.AspNetCore.Identity;

namespace AgoraFold.Core.Entities;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";

    public ICollection<Listing> Listings { get; set; } = [];
}
