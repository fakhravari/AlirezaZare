using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Shop.Data.Application.Email.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;
using Shop.Models;

namespace Shop.Controllers.Api;

[ApiController]
[AllowAnonymous]
[Route("api/store/contact")]
public sealed class StoreContactApiController(
    IEmailSender emailSender,
    IOptions<SiteIdentityOptions> siteIdentity,
    ShopDbContext db,
    ILogger<StoreContactApiController> logger) : ControllerBase
{
    private static readonly HashSet<string> Subjects = new(StringComparer.Ordinal)
    {
        "order", "consultation", "after-sales", "business", "other"
    };

    [HttpPost]
    [EnableRateLimiting("contact-form")]
    public async Task<IActionResult> Submit([FromBody] ContactRequest request, CancellationToken ct)
    {
        string name = (request.Name ?? string.Empty).Trim();
        string phone = NormalizePhone(request.Phone);
        string subject = (request.Subject ?? string.Empty).Trim();
        string message = (request.Message ?? string.Empty).Trim();
        if (name.Length is < 3 or > 100 || name.Any(char.IsControl) ||
            phone.Length != 11 || !phone.StartsWith("09", StringComparison.Ordinal) ||
            !Subjects.Contains(subject) || message.Length is < 10 or > 3000 || message.Any(character => character == '\0'))
            return BadRequest(new { code = "invalid-contact-request", message = "اطلاعات فرم تماس را بررسی و کامل کنید." });

        var contactMessage = new StoreContactMessage
        {
            Name = name,
            Phone = phone,
            SubjectKey = subject,
            Message = message,
            Status = "new",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.StoreContactMessages.Add(contactMessage);
        await db.SaveChangesAsync(ct);

        string subjectLabel = subject switch
        {
            "order" => "پیگیری سفارش",
            "consultation" => "مشاوره پیش از خرید",
            "after-sales" => "خدمات پس از فروش",
            "business" => "همکاری سازمانی",
            _ => "سایر درخواست‌ها"
        };
        string body = $"درخواست جدید از فرم تماس سایت\n\nنام: {name}\nشماره همراه: {phone}\nموضوع: {subjectLabel}\n\nمتن پیام:\n{message}";
        string recipient = siteIdentity.Value.PublicEmail?.Trim() ?? string.Empty;
        bool notificationSent = false;
        try
        {
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                recipient = new MailAddress(recipient).Address;
                await emailSender.SendAsync(recipient, $"پیام سایت: {subjectLabel}", body, ct);
                notificationSent = true;
            }
            else logger.LogWarning("Contact message {ContactMessageId} was saved, but no public email recipient is configured.", contactMessage.Id);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Contact message {ContactMessageId} was saved, but email notification failed.", contactMessage.Id);
        }

        return Ok(new { saved = true, notificationSent, message = "پیام شما ثبت شد و برای پیگیری در پنل پشتیبانی قرار گرفت." });
    }

    private static string NormalizePhone(string? value)
    {
        var digits = new System.Text.StringBuilder();
        foreach (char character in value ?? string.Empty)
        {
            if (character is >= '0' and <= '9') digits.Append(character);
            else if (character is >= '۰' and <= '۹') digits.Append((char)('0' + character - '۰'));
            else if (character is >= '٠' and <= '٩') digits.Append((char)('0' + character - '٠'));
        }
        return digits.ToString();
    }

    public sealed record ContactRequest(string? Name, string? Phone, string? Subject, string? Message);
}
