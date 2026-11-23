namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreContactMessage
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string SubjectKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "new";
    public ContactMessageStatusReference StatusReference { get; set; } = null!;
    public string? AdminReply { get; set; }
    public DateTime? RepliedAtUtc { get; set; }
    public DateTime? ReplyEmailSentAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
