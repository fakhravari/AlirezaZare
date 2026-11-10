namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class Warehouse
{
    public long Id { get; set; }
    public long IdCompany { get; set; }
    public long? IdOwnerSalesPlanReference { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSalesWarehouse { get; set; }
    public bool IsActive { get; set; }
    public Company Company { get; set; } = null!;
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
}