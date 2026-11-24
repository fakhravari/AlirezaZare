namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class InventoryItem
{
    public decimal Id { get; set; }
    public decimal IdProduct { get; set; }
    public long? IdProductVariant { get; set; }
    public bool IsActive { get; set; }
    public WarehouseProduct Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
}