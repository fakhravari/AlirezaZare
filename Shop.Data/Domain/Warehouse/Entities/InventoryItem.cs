namespace Shop.Database.Domain.Warehouse.Entities;

public sealed class InventoryItem
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public long? IdProductVariant { get; set; }
    public WarehouseProduct Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
}
