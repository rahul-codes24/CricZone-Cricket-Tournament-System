namespace KplTournament.Web.Models;

public class AdminUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;

    public string? OtpCode { get; set; }
    public DateTime? OtpExpiry { get; set; }

    // Basic brute-force protection
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
}
