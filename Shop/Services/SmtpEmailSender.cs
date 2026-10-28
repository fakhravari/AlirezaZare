using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Shop.Services;

public sealed class SmtpEmailSender(IOptions<Models.EmailSmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly Models.EmailSmtpOptions _settings = options.Value;

    public async Task SendVerificationCodeAsync(string recipient, string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host)
            || _settings.Port is < 1 or > 65535
            || string.IsNullOrWhiteSpace(_settings.Username)
            || string.IsNullOrWhiteSpace(_settings.From)
            || string.IsNullOrWhiteSpace(_settings.Password))
        {
            throw new InvalidOperationException("SMTP settings are incomplete.");
        }

        using var message = new MailMessage(_settings.From, recipient)
        {
            Subject = "کد تأیید فروشگاه برق شیراز",
            Body = $"کد تأیید شما: {code}\n\nاین کد تا ۱۰ دقیقه معتبر است. اگر این درخواست از طرف شما نبوده، این پیام را نادیده بگیرید.",
            IsBodyHtml = false,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8
        };
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            Timeout = 15000,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpException or OperationCanceledException)
        {
            logger.LogWarning("Verification email delivery failed. Failure type: {FailureType}", exception.GetType().Name);
            throw;
        }
    }
}
