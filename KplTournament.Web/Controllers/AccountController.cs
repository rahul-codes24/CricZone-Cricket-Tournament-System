using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Net.Http;
using System.Threading.Tasks;
using KplTournament.Web.Data;
using KplTournament.Web.ViewModels;
using KplTournament.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KplTournament.Web.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public AccountController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login()
    {
        ViewBag.LoginModel = new LoginViewModel();
        ViewBag.RegisterModel = new RegisterViewModel();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewBag.LoginModel = model;
        ViewBag.RegisterModel = new RegisterViewModel();

        if (!ModelState.IsValid) return View("Login");

        var fullPhone = model.CountryCode + model.MobileNumber;
        var user = await _db.AdminUsers.FirstOrDefaultAsync(x => x.MobileNumber == fullPhone || x.Email == fullPhone);

        if (user == null)
        {
            ViewBag.LoginError = "This mobile number is not registered. Please sign up using the form.";
            return View("Login");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            var minutesLeft = Math.Ceiling((user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes);
            ViewBag.LoginError = $"Too many failed attempts. Try again in {minutesLeft} minute(s).";
            return View("Login");
        }

        var computedHash = SeedData.Hash(model.Password, user.PasswordSalt);
        var isValid = CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(computedHash),
            Convert.FromHexString(user.PasswordHash));

        if (!isValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginAttempts = 0;
            }
            await _db.SaveChangesAsync();

            ViewBag.LoginError = "Wrong mobile number or password.";
            return View("Login");
        }

        // Successful credential check — Reset failed attempts and lockout
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        await _db.SaveChangesAsync();

        // Sign in immediately (No OTP)
        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(ClaimTypes.Name, user.Email),
            new System.Security.Claims.Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4)
        });

        var hasTournaments = await _db.Tournaments.AnyAsync(t => t.AdminEmail == user.Email);
        if (hasTournaments)
        {
            return RedirectToAction("Tournaments", "Admin");
        }
        else
        {
            return RedirectToAction("CreateTournament", "Admin");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewBag.LoginModel = new LoginViewModel();
        ViewBag.RegisterModel = model;

        if (!ModelState.IsValid) return View("Login");

        var fullPhone = model.CountryCode + model.MobileNumber;
        var existing = await _db.AdminUsers.AnyAsync(x => x.MobileNumber == fullPhone || x.Email == fullPhone);
        if (existing)
        {
            ViewBag.RegisterError = "This mobile number is already registered.";
            return View("Login");
        }

        var salt = SeedData.GenerateSalt();
        var passwordHash = SeedData.Hash(model.Password, salt);

        var newUser = new AdminUser
        {
            Email = fullPhone,
            Name = model.Name ?? "Organizer",
            MobileNumber = fullPhone,
            PasswordHash = passwordHash,
            PasswordSalt = salt,
            FailedLoginAttempts = 0
        };

        _db.AdminUsers.Add(newUser);
        await _db.SaveChangesAsync();

        // Sign in immediately (No OTP)
        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(ClaimTypes.Name, newUser.Email),
            new System.Security.Claims.Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4)
        });

        var hasTournaments = await _db.Tournaments.AnyAsync(t => t.AdminEmail == newUser.Email);
        if (hasTournaments)
        {
            return RedirectToAction("Tournaments", "Admin");
        }
        else
        {
            return RedirectToAction("CreateTournament", "Admin");
        }
    }

    [HttpGet]
    public async Task<IActionResult> VerifyOtp(string email)
    {
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(OtpViewModel model)
    {
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult VerifyRegisterOtp(string email)
    {
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyRegisterOtp(OtpViewModel model)
    {
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string countryCode, string mobileNumber, string newPassword, string confirmPassword)
    {
        ViewBag.LoginModel = new LoginViewModel();
        ViewBag.RegisterModel = new RegisterViewModel();

        var fullPhone = countryCode + mobileNumber;
        var user = await _db.AdminUsers.FirstOrDefaultAsync(x => x.MobileNumber == fullPhone || x.Email == fullPhone);
        if (user == null)
        {
            ViewBag.LoginError = "This mobile number is not registered.";
            return View("Login");
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.LoginError = "Passwords do not match.";
            return View("Login");
        }

        var regex = new System.Text.RegularExpressions.Regex(@"^(?=.*[a-zA-Z])(?=.*\d)[a-zA-Z\d]{6,10}$");
        if (!regex.IsMatch(newPassword))
        {
            ViewBag.LoginError = "Password must be 6-10 characters long, mix letters and numbers, and contain no special symbols.";
            return View("Login");
        }

        var salt = SeedData.GenerateSalt();
        user.PasswordSalt = salt;
        user.PasswordHash = SeedData.Hash(newPassword, salt);
        await _db.SaveChangesAsync();

        ViewBag.LoginSuccess = "Password reset successfully! Please sign in with your new password.";
        return View("Login");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Landing");
    }
}
