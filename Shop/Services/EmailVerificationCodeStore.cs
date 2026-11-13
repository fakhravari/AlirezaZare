using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Shop.Services;

public sealed class EmailVerificationCodeStore
{
    private const int MaximumAttempts = 5;
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public EmailVerificationCodeStore()
    {
    }

    public bool TryCreate(string email, int lifetimeMinutes, out string code, out TimeSpan retryAfter)
    {
        if (lifetimeMinutes is < 1 or > 1440)
            throw new ArgumentOutOfRangeException(nameof(lifetimeMinutes),
                "Email verification code lifetime must be between 1 and 1440 minutes.");
        var now = DateTimeOffset.UtcNow;
        if (_entries.TryGetValue(email, out var existing) && existing.SentAt + TimeSpan.FromMinutes(existing.LifetimeMinutes) > now)
        {
            code = "";
            retryAfter = existing.SentAt + TimeSpan.FromMinutes(existing.LifetimeMinutes) - now;
            return false;
        }

        code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _entries[email] = new Entry(Hash(code), now, now.AddMinutes(lifetimeMinutes), lifetimeMinutes, 0);
        retryAfter = TimeSpan.Zero;
        return true;
    }

    public void Remove(string email) => _entries.TryRemove(email, out _);

    public bool Verify(string email, string code)
    {
        if (!_entries.TryGetValue(email, out var entry)) return false;
        var now = DateTimeOffset.UtcNow;
        if (entry.ExpiresAt <= now || entry.Attempts >= MaximumAttempts)
        {
            _entries.TryRemove(email, out _);
            return false;
        }

        var candidate = Hash(code);
        if (CryptographicOperations.FixedTimeEquals(candidate, entry.CodeHash))
        {
            _entries.TryRemove(email, out _);
            return true;
        }

        _entries[email] = entry with { Attempts = entry.Attempts + 1 };
        return false;
    }

    private static byte[] Hash(string code) => SHA256.HashData(Encoding.UTF8.GetBytes(code));

    private sealed record Entry(byte[] CodeHash, DateTimeOffset SentAt, DateTimeOffset ExpiresAt, int LifetimeMinutes, int Attempts);
}
