using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Services;

public sealed class SmtpEmailSender(ShopDbContext db, IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    : IEmailSender
{
    public async Task SendVerificationCodeAsync(string recipient, string code, CancellationToken cancellationToken)
    {
        var settings = await db.StoreSiteIdentities.AsNoTracking()
            .Where(x => x.Id == 1)
            .Select(x => new
            {
                x.SmtpHost, x.SmtpPort, x.SmtpEnableSsl, x.SmtpUsername, x.SmtpFrom, x.SmtpPassword
            })
            .SingleOrDefaultAsync(cancellationToken);
        string? password = settings?.SmtpPassword ?? configuration["Email:Smtp:Password"];
        if (settings is null || string.IsNullOrWhiteSpace(settings.SmtpHost)
            || settings.SmtpPort is null or < 1 or > 65535 || settings.SmtpEnableSsl is null
            || string.IsNullOrWhiteSpace(settings.SmtpUsername)
            || string.IsNullOrWhiteSpace(settings.SmtpFrom)
            || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("SMTP settings are incomplete.");
        }

        using var message = new MailMessage(settings.SmtpFrom, recipient)
        {
            Subject = "کد تأیید فروشگاه برق شیراز",
            Body =
                $"کد تأیید شما: {code}\n\nاین کد تا ۱۰ دقیقه معتبر است. اگر این درخواست از طرف شما نبوده، این پیام را نادیده بگیرید.",
            IsBodyHtml = false,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8
        };
        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort.Value)
        {
            EnableSsl = settings.SmtpEnableSsl.Value,
            Credentials = new NetworkCredential(settings.SmtpUsername, password),
            Timeout = 15000,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpException or OperationCanceledException)
        {
            logger.LogWarning("Verification email delivery failed. Failure type: {FailureType}",
                exception.GetType().Name);
            throw;
        }
    }
}
