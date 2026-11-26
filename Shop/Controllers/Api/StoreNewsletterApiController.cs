using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Controllers.Api;

[ApiController]
[AllowAnonymous]
[Route("api/store/newsletter")]
public sealed class StoreNewsletterApiController(IStorePublicPageData pageData) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("newsletter")]
    public async Task<IActionResult> Subscribe([FromBody] NewsletterRequest request, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out long profileId))
                return Unauthorized();

            if (!await pageData.EnableAccountNewsletterAsync(profileId, ct)) return Unauthorized();

            return Ok(new { subscribed = true, forAccount = true });
        }

        string email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email))
            return BadRequest(new { code = "invalid-email", message = "نشانی ایمیل معتبر نیست." });

        bool created = await pageData.SubscribeNewsletterAsync(email, ct);

        return Ok(new { subscribed = true, alreadySubscribed = !created });
    }

    private static string NormalizeEmail(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        if (email.Length is < 3 or > 254) return false;

        try
        {
            return new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public sealed record NewsletterRequest(string? Email);
}
