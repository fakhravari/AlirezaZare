namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreProductBadge
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#102A43";
}
