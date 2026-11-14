namespace Shop.Data.Domain.Account.Entities;

public sealed class EmailVerificationCode
{
    public string Email { get; set; } = "";
    public byte[] CodeHash { get; set; } = [];
    public DateTime SentAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int LifetimeMinutes { get; set; }
    public int Attempts { get; set; }
}
