using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Data.Application.Email.Contracts;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Email;

public sealed class SmtpEmailSender(
    ShopDbContext db,
    IConfiguration configuration,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentNullException.ThrowIfNull(body);

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
            Subject = subject,
            Body = body,
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
            logger.LogWarning("Email delivery failed. Failure type: {FailureType}", exception.GetType().Name);
            throw;
        }
    }
}
