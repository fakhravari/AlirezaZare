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
        if (!IsValidEmail(email) || password.Length is < 10 or > 128)
            return Unauthorized(new { code = "invalid-credentials", message = "ایمیل یا رمز عبور درست نیست." });

        string? passwordHash = await db.Database.SqlQueryRaw<string?>(
                "SELECT [PasswordHash] AS [Value] FROM [Account].[Profile] WHERE [Email] = {0}", email)
            .SingleOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(passwordHash) ||
            passwordHasher.VerifyHashedPassword(email, passwordHash, password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { code = "invalid-credentials", message = "ایمیل یا رمز عبور درست نیست." });

        long profileId = await db.Database.SqlQueryRaw<long>(
                "SELECT [Id] AS [Value] FROM [Account].[Profile] WHERE [Email] = {0}", email)
            .SingleAsync(ct);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, profileId.ToString()),
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
