using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Warehouse.Entities;

namespace Shop.Data.Domain.Store.Entities;

public sealed class ProductFeedback
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public long? IdProfile { get; set; }
    public string FeedbackType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FeedbackText { get; set; } = string.Empty;
    public byte? Rating { get; set; }
    public int IdStatus { get; set; } = ProductFeedbackStatus.Pending;
    public ProductFeedbackStatus Status { get; set; } = null!;
    public string? AdminReply { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RepliedAtUtc { get; set; }
    public WarehouseProduct Product { get; set; } = null!;
    public AccountProfile? Profile { get; set; }
}

public sealed class ProductFeedbackStatus
{
    public const int Pending = 1;
    public const int Approved = 2;
    public const int Rejected = 3;
    public const int Deleted = 4;
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NameEn { get; set; } = "";
}
