namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class WarehouseProduct
{
    public decimal Id { get; set; }
    public string ProductKey { get; set; } = string.Empty;
    public string NameFa { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public long? IdBrand { get; set; }
    public long? IdCategory { get; set; }
    public int MaxPurchaseQuantity { get; set; }
    public string? Slug { get; set; }
    public decimal? IdBadge { get; set; }
    public Shop.Data.Domain.Store.Entities.StoreProductBadge? ProductBadge { get; set; }
    public bool IsStoreEnabled { get; set; }
    public bool IsWarehouseEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
