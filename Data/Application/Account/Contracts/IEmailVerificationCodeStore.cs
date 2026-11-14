namespace Shop.Data.Application.Account.Contracts;

public interface IEmailVerificationCodeStore
{
    Task<EmailCodeCreateResult> TryCreateAsync(string email, int lifetimeMinutes,
        CancellationToken cancellationToken);

    Task RemoveAsync(string email, CancellationToken cancellationToken);
    Task<bool> VerifyAsync(string email, string code, CancellationToken cancellationToken);
}

public sealed record EmailCodeCreateResult(bool Created, string? Code, TimeSpan RetryAfter, int LifetimeMinutes);
