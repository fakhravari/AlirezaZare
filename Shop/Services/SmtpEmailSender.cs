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
        var settings = await db.Database.SqlQueryRaw<SmtpSettings>("""
            SELECT [SmtpHost] AS [Host], [SmtpPort] AS [Port], [SmtpEnableSsl] AS [EnableSsl],
                   [SmtpUsername] AS [Username], [SmtpFrom] AS [From]
            FROM [Store].[SiteSetting] WHERE [Id] = 1
            """).SingleOrDefaultAsync(cancellationToken);
        string? password = configuration["Email:Smtp:Password"];
        if (settings is null || string.IsNullOrWhiteSpace(settings.Host)
            || settings.Port is null or < 1 or > 65535 || settings.EnableSsl is null
            || string.IsNullOrWhiteSpace(settings.Username)
            || string.IsNullOrWhiteSpace(settings.From)
            || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("SMTP settings are incomplete.");
        }

        using var message = new MailMessage(settings.From, recipient)
        {
            Subject = "کد تأیید فروشگاه برق شیراز",
            Body =
                $"کد تأیید شما: {code}\n\nاین کد تا ۱۰ دقیقه معتبر است. اگر این درخواست از طرف شما نبوده، این پیام را نادیده بگیرید.",
            IsBodyHtml = false,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8
        };
        using var client = new SmtpClient(settings.Host, settings.Port.Value)
        {
            EnableSsl = settings.EnableSsl.Value,
            Credentials = new NetworkCredential(settings.Username, password),
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

    private sealed class SmtpSettings
    {
        public string? Host { get; set; }
        public int? Port { get; set; }
        public bool? EnableSsl { get; set; }
        public string? Username { get; set; }
        public string? From { get; set; }
    }
}
