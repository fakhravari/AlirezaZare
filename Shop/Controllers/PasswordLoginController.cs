using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Controllers;

[ApiController]
[Route("api/auth/password")]
public sealed class PasswordLoginController(
    ShopDbContext db,
    IPasswordHasher<string> passwordHasher) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("email-code")]
    public async Task<IActionResult> Login([FromBody] PasswordLoginRequest request, CancellationToken ct)
    {
        string email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        string password = request.Password ?? string.Empty;
        if (!IsValidEmail(email) || string.IsNullOrEmpty(password))
            return Unauthorized(new { code = "invalid-credentials", message = "ایمیل یا رمز عبور درست نیست." });

        var profile = await db.AccountProfiles.AsNoTracking()
            .Where(x => x.Email == email)
            .Select(x => new { x.Id, x.Password })
            .SingleOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(profile?.Password) ||
            (!profile.Password.StartsWith("AQAAAA", StringComparison.Ordinal) && profile.Password != password) ||
            (profile.Password.StartsWith("AQAAAA", StringComparison.Ordinal) &&
             passwordHasher.VerifyHashedPassword(email, profile.Password, password) == PasswordVerificationResult.Failed))
            return Unauthorized(new { code = "invalid-credentials", message = "ایمیل یا رمز عبور درست نیست." });

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, profile!.Id.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, email)
        };
        await HttpContext.SignInAsync("ShopCookie", new ClaimsPrincipal(new ClaimsIdentity(claims, "ShopCookie")));
        return Ok(new { authenticated = true, email });
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            return email.Length <= 254 &&
                   new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed record PasswordLoginRequest(string? Email, string? Password);
