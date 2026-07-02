using System.ComponentModel.DataAnnotations;

namespace KioskRewards.Web.Models;

/// <summary>
/// Just the fields from the login form, plus a return url so we can send the member back where
/// they came from after logging in.
/// </summary>
public sealed class LoginFormModel
{
    [Required(ErrorMessage = "Username is required.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    /// Where to send the member after they log in successfully
    public string? ReturnUrl { get; set; }
}
