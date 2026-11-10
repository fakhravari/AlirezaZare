namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class WarehouseProduct
{
    public long Id { get; set; }
    public long IdBaseUnit { get; set; }
    public string ProductKey { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? EnglishName { get; set; }
    public long? IdBrand { get; set; }
    public long? IdCategory { get; set; }
    public string? PowerSource { get; set; }
    public string? UseCase { get; set; }
    public decimal? PriceAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public int? MaxPurchaseQuantity { get; set; }
    public string? Slug { get; set; }
    public string? Badge { get; set; }
    public bool IsNewest { get; set; }
    public int? NewestOrder { get; set; }
    public bool IsStoreEnabled { get; set; } = true;
    public bool IsCatalogActive { get; set; } = true;
    public bool IsWarehouseEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
