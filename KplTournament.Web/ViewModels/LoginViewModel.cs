using System.ComponentModel.DataAnnotations;

namespace KplTournament.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Mobile number must be exactly 10 digits.")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country code is required.")]
    public string CountryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
