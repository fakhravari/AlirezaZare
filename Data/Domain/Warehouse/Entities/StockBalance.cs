namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class StockBalance
{
    public long Id { get; set; }
    public long IdWarehouse { get; set; }
    public long IdInventoryItem { get; set; }
    public long IdSalesPlanReference { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinimumQuantity { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
    public Warehouse Warehouse { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
    public SalesPlanReference SalesPlanReference { get; set; } = null!;
}
