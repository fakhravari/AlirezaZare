using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Shop.Services;

public sealed class EmailVerificationCodeStore
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendDelay = TimeSpan.FromSeconds(60);
    private const int MaximumAttempts = 5;
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public bool TryCreate(string email, out string code, out TimeSpan retryAfter)
    {
        var now = DateTimeOffset.UtcNow;
        if (_entries.TryGetValue(email, out var existing) && existing.SentAt + ResendDelay > now)
        {
            code = "";
            retryAfter = existing.SentAt + ResendDelay - now;
            return false;
        }

        code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _entries[email] = new Entry(Hash(code), now, now + CodeLifetime, 0);
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
    private sealed record Entry(byte[] CodeHash, DateTimeOffset SentAt, DateTimeOffset ExpiresAt, int Attempts);
}
