using System.ComponentModel.DataAnnotations;

namespace KplTournament.Web.ViewModels
{
    public class OtpViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be numeric.")]
        public string OtpCode { get; set; } = string.Empty;
    }
}
