using System.ComponentModel.DataAnnotations;

namespace AgoraFold.Mvc.Models.Account;

public class RegisterViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Display name")]
    public string DisplayName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = "";
}
