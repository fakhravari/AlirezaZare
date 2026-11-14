using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.Data.Application.Account.Contracts;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Identity;

public sealed class DatabaseEmailVerificationCodeStore(ShopDbContext db) : IEmailVerificationCodeStore
{
    private const int MaximumAttempts = 5;

    public async Task<EmailCodeCreateResult> TryCreateAsync(string email, int lifetimeMinutes,
        CancellationToken cancellationToken)
    {
        if (lifetimeMinutes is < 1 or > 1440)
            throw new ArgumentOutOfRangeException(nameof(lifetimeMinutes),
                "Email verification code lifetime must be between 1 and 1440 minutes.");

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,
            cancellationToken);
        await AcquireEmailLockAsync(email, cancellationToken);
        DateTime now = DateTime.UtcNow;
        await db.EmailVerificationCodes.Where(item => item.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync(cancellationToken);
        EmailVerificationCode? existing = await db.EmailVerificationCodes.SingleOrDefaultAsync(
            item => item.Email == email, cancellationToken);
        if (existing is not null && existing.ExpiresAtUtc > now)
        {
            TimeSpan retryAfter = existing.ExpiresAtUtc - now;
            await transaction.CommitAsync(cancellationToken);
            return new(false, null, retryAfter, existing.LifetimeMinutes);
        }

        string code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        if (existing is null)
        {
            existing = new EmailVerificationCode { Email = email };
            db.EmailVerificationCodes.Add(existing);
        }
        existing.CodeHash = Hash(code);
        existing.SentAtUtc = now;
        existing.ExpiresAtUtc = now.AddMinutes(lifetimeMinutes);
        existing.LifetimeMinutes = lifetimeMinutes;
        existing.Attempts = 0;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, code, TimeSpan.Zero, lifetimeMinutes);
    }

    public async Task RemoveAsync(string email, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        await AcquireEmailLockAsync(email, cancellationToken);
        await db.EmailVerificationCodes.Where(item => item.Email == email)
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> VerifyAsync(string email, string code, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,
            cancellationToken);
        await AcquireEmailLockAsync(email, cancellationToken);
        EmailVerificationCode? entry = await db.EmailVerificationCodes.SingleOrDefaultAsync(
            item => item.Email == email, cancellationToken);
        if (entry is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        if (entry.ExpiresAtUtc <= DateTime.UtcNow || entry.Attempts >= MaximumAttempts)
        {
            db.EmailVerificationCodes.Remove(entry);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        if (CryptographicOperations.FixedTimeEquals(Hash(code), entry.CodeHash))
        {
            db.EmailVerificationCodes.Remove(entry);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        entry.Attempts++;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return false;
    }

    private static byte[] Hash(string code) => SHA256.HashData(Encoding.UTF8.GetBytes(code));

    private async Task AcquireEmailLockAsync(string email, CancellationToken cancellationToken)
    {
        string resource = "EmailVerificationCode:" + Convert.ToHexString(Hash(email));
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction()
                             ?? throw new InvalidOperationException("Email verification requires an active transaction.");
        command.CommandText = "DECLARE @lockResult int; " +
                              "EXEC @lockResult = sys.sp_getapplock @Resource = @resource, " +
                              "@LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000; " +
                              "SELECT @lockResult;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.DbType = DbType.String;
        parameter.Size = 100;
        parameter.Value = resource;
        command.Parameters.Add(parameter);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || Convert.ToInt32(result) < 0)
            throw new TimeoutException("Could not acquire the email verification lock.");
    }
}
