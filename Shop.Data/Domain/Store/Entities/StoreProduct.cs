namespace Shop.Database.Domain.Store.Entities;

public sealed class StoreProduct
{
    public long Id { get; set; }
    public string StoreProductKey { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public ICollection<ProductFeedback> Feedback { get; set; } = new List<ProductFeedback>();
}
