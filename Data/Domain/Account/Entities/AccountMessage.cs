namespace Shop.Data.Domain.Account.Entities;

public sealed class AccountMessage
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "notice";
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
