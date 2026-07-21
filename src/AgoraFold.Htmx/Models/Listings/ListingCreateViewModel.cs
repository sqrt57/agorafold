using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AgoraFold.Htmx.Models.Listings;

public class ListingCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = "";

    [Range(0, 100_000_000)]
    public decimal? Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];

    [ValidateNever]
    [Display(Name = "Images")]
    public List<IFormFile>? Images { get; set; }
}
