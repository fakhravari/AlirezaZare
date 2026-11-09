using Shop.Database.Domain.Account.Entities;

namespace Shop.Database.Domain.Store.Entities;

public sealed class ProductFeedback
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public long? IdProfile { get; set; }
    public string FeedbackType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FeedbackText { get; set; } = string.Empty;
    public byte? Rating { get; set; }
    public string Status { get; set; } = "pending";
    public string? AdminReply { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RepliedAtUtc { get; set; }
    public StoreProduct Product { get; set; } = null!;
    public AccountProfile? Profile { get; set; }
}