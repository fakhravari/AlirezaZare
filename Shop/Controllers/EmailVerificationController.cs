using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Email.Contracts;
using Shop.Data.Infrastructure.Persistence;
using Shop.Services;

namespace Shop.Controllers;

[ApiController]
[Route("api/auth/email-code")]
public sealed partial class EmailVerificationController(
    EmailVerificationCodeStore codes,
    IEmailSender emailSender,
    ShopDbContext db) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("email-code")]
    public async Task<IActionResult> Send([FromBody] EmailCodeRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email)) return BadRequest(new { code = "invalid-email", message = "ایمیل معتبر نیست." });
        var lifetimeMinutes = await db.StoreSiteIdentities.AsNoTracking()
            .Where(x => x.Id == 1).Select(x => x.EmailVerificationCodeLifetimeMinutes)
            .SingleOrDefaultAsync(cancellationToken);
        if (lifetimeMinutes is < 1 or > 1440) lifetimeMinutes = 15;
        if (!codes.TryCreate(email, lifetimeMinutes, out var code, out var retryAfter))
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new
                {
                    code = "rate-limited",
                    message = $"در {lifetimeMinutes} دقیقهٔ گذشته برای این ایمیل کد ارسال شده است؛ کد قبلی تا پایان این زمان معتبر است.",
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                });
        try
        {
            var emailBody = $"کد تأیید شما: {code}\n\nاین کد تا {lifetimeMinutes} دقیقه معتبر است. اگر این درخواست از طرف شما نبوده، این پیام را نادیده بگیرید.";
            await emailSender.SendAsync(email, "کد تأیید فروشگاه برق شیراز", emailBody, cancellationToken);
            return Ok(new { message = $"ایمیل حاوی کد تأیید ارسال شد؛ کد تا {lifetimeMinutes} دقیقه اعتبار دارد و در این مدت ایمیل دیگری ارسال نمی‌شود." });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            codes.Remove(email);
            return StatusCode(499);
        }
        catch
        {
            codes.Remove(email);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { code = "email-unavailable", message = "ارسال ایمیل در حال حاضر ممکن نیست." });
        }
    }

    [HttpPost("verify")]
    [EnableRateLimiting("email-code")]
    public async Task<IActionResult> Verify([FromBody] VerifyEmailCodeRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email) || !SixDigitCode().IsMatch(request.Code ?? ""))
            return BadRequest(new { code = "invalid-code" });
        bool isRegistration = string.Equals(request.Mode, "register", StringComparison.Ordinal);
        if (request.Mode is not ("login" or "register"))
            return BadRequest(new { code = "invalid-mode", message = "روش تأیید معتبر نیست." });
        if (isRegistration && !IsValidPassword(request.Password))
            return BadRequest(new { code = "invalid-password", message = "رمز عبور باید دست‌کم ۱ نویسه داشته باشد." });
        if (!isRegistration && request.Password is not null)
            return BadRequest(new { code = "invalid-request" });
        if (!codes.Verify(email, request.Code!)) return BadRequest(new { code = "invalid-code" });
        var profile = await db.AccountProfiles.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (profile is null)
        {
            profile = new() { Email = email, CreatedAtUtc = DateTime.UtcNow };
            db.AccountProfiles.Add(profile);
        }
        if (isRegistration)
        {
            profile.Password = request.Password;
        }
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, profile.Id.ToString()), new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, email)
        };
        await HttpContext.SignInAsync("ShopCookie", new ClaimsPrincipal(new ClaimsIdentity(claims, "ShopCookie")));
        return Ok(new { authenticated = true, email });
    }

    [HttpGet("session")]
    public IActionResult Session() => User.Identity?.IsAuthenticated == true
        ? Ok(new { authenticated = true, email = User.FindFirstValue(ClaimTypes.Email) })
        : Ok(new { authenticated = false });

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("ShopCookie");
        return Ok(new { authenticated = false });
    }

    private static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

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

    private static bool IsValidPassword(string? password) =>
        !string.IsNullOrEmpty(password) && password.Length <= 500;

    [GeneratedRegex("^[0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex SixDigitCode();
}

public sealed record EmailCodeRequest(string? Email);

public sealed record VerifyEmailCodeRequest(string? Email, string? Code, string? Mode, string? Password);
