namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreProductBadge
{
    public decimal Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#102A43";
    public string TextColor { get; set; } = "#FFFFFF";
    public string BorderColor { get; set; } = "#FFFFFF";
}