namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreHomeTestimonial
{
    public long IdProductFeedback { get; set; }
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ProductFeedback Feedback { get; set; } = null!;
}
