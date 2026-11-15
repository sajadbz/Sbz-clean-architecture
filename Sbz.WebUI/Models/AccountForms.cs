using System.ComponentModel.DataAnnotations;

namespace Sbz.WebUI.Models;

public class LoginForm
{
    [Required, StringLength(256)]
    [Display(Name = "Username")]
    public string UserName { get; set; }

    [Required, StringLength(128), DataType(DataType.Password)]
    public string Password { get; set; }

    public string ReturnUrl { get; set; }
}

public sealed class RegistrationForm
{
    [Required, StringLength(32, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_.-]+$", ErrorMessage = "Use letters, numbers, periods, underscores or hyphens.")]
    [Display(Name = "Username")]
    public string UserName { get; set; }

    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password)]
    public string Password { get; set; }

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; }
}
