using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Controllers.Api;

[ApiController]
[AllowAnonymous]
[Route("api/store/newsletter")]
public sealed class StoreNewsletterApiController(ShopDbContext db) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("newsletter")]
    public async Task<IActionResult> Subscribe([FromBody] NewsletterRequest request, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out long profileId))
                return Unauthorized();

            var profile = await db.AccountProfiles.SingleOrDefaultAsync(x => x.Id == profileId, ct);
            if (profile is null) return Unauthorized();
            profile.NewsletterEnabled = true;
            profile.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Ok(new { subscribed = true, forAccount = true });
        }

        string email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email))
            return BadRequest(new { code = "invalid-email", message = "نشانی ایمیل معتبر نیست." });

        if (await db.StoreNewsletterSubscribers.AnyAsync(x => x.Email == email, ct))
            return Ok(new { subscribed = true, alreadySubscribed = true });

        var subscriber = new StoreNewsletterSubscriber { Email = email, SubscribedAtUtc = DateTime.UtcNow };
        db.StoreNewsletterSubscribers.Add(subscriber);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.Entry(subscriber).State = EntityState.Detached;
            if (!await db.StoreNewsletterSubscribers.AnyAsync(x => x.Email == email, ct)) throw;
            return Ok(new { subscribed = true, alreadySubscribed = true });
        }

        return Ok(new { subscribed = true, alreadySubscribed = false });
    }

    private static string NormalizeEmail(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        if (email.Length is < 3 or > 254) return false;
        try { return new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase); }
        catch (FormatException) { return false; }
    }

    public sealed record NewsletterRequest(string? Email);
}
