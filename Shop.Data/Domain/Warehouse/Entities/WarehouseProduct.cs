namespace Shop.Database.Domain.Warehouse.Entities;

public sealed class WarehouseProduct
{
    public long Id { get; set; }
    public long IdBaseUnit { get; set; }
    public long IdStoreProduct { get; set; }
    public string StoreProductKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}