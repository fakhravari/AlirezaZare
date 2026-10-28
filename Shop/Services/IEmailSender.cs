namespace Shop.Services;

public interface IEmailSender
{
    Task SendVerificationCodeAsync(string recipient, string code, CancellationToken cancellationToken);
}
