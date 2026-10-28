using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shop.Services;

namespace Shop.Controllers;

[ApiController]
[EnableRateLimiting("email-code")]
[Route("api/auth/email-code")]
public sealed partial class EmailVerificationController(EmailVerificationCodeStore codes, IEmailSender emailSender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] EmailCodeRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email)) return BadRequest(new { message = "ایمیل را با قالب صحیح وارد کنید." });
        if (!codes.TryCreate(email, out var code, out var retryAfter))
        {
            var retrySeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = $"برای ارسال دوباره {retrySeconds} ثانیه صبر کنید." });
        }

        try
        {
            await emailSender.SendVerificationCodeAsync(email, code, cancellationToken);
            return Ok(new { message = "اگر ارسال ایمیل موفق باشد، کد تأیید تا ۱۰ دقیقه معتبر است." });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            codes.Remove(email);
            return StatusCode(499);
        }
        catch
        {
            codes.Remove(email);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "ارسال ایمیل در حال حاضر ممکن نشد. تنظیمات SMTP را بررسی کنید و دوباره تلاش کنید." });
        }
    }

    [HttpPost("verify")]
    public IActionResult Verify([FromBody] VerifyEmailCodeRequest request)
    {
        var email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email) || !SixDigitCode().IsMatch(request.Code ?? ""))
            return BadRequest(new { message = "ایمیل یا کد تأیید معتبر نیست." });
        return codes.Verify(email, request.Code!)
            ? Ok(new { message = "ایمیل تأیید شد." })
            : BadRequest(new { message = "کد تأیید اشتباه است یا اعتبار آن پایان یافته است." });
    }

    private static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        try { return email.Length <= 254 && new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase); }
        catch (FormatException) { return false; }
    }

    [GeneratedRegex("^[0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex SixDigitCode();
}

public sealed record EmailCodeRequest(string? Email);
public sealed record VerifyEmailCodeRequest(string? Email, string? Code);
