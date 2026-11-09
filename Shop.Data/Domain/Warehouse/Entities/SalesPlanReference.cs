namespace Shop.Database.Domain.Warehouse.Entities;

public sealed class SalesPlanReference
{
    public long Id { get; set; }
    public string StoreSalesPlanKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsRegular { get; set; }
    public bool IsActive { get; set; }
    public byte IdStockOwnerType { get; set; }
    public string Code { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
}

public sealed class WarehouseStockOwner
{
    public long IdWarehouse { get; set; }
    public long IdStockOwner { get; set; }
}
