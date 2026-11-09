using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;
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
        if (!codes.TryCreate(email, out var code, out var retryAfter))
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new
                {
                    code = "rate-limited", retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                });
        try
        {
            await emailSender.SendVerificationCodeAsync(email, code, cancellationToken);
            return Ok(new { message = "کد تأیید ارسال شد و تا ۱۰ دقیقه اعتبار دارد." });
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
        if (!codes.Verify(email, request.Code!)) return BadRequest(new { code = "invalid-code" });
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"IF NOT EXISTS(SELECT 1 FROM [Account].[Profile] WHERE [Email]={email}) INSERT INTO [Account].[Profile]([Email]) VALUES({email});",
            cancellationToken);
        long id = await db.Database
            .SqlQueryRaw<long>("SELECT [Id] AS [Value] FROM [Account].[Profile] WHERE [Email] = {0}", email)
            .SingleAsync(cancellationToken);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Email, email),
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

    [GeneratedRegex("^[0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex SixDigitCode();
}

public sealed record EmailCodeRequest(string? Email);

public sealed record VerifyEmailCodeRequest(string? Email, string? Code);