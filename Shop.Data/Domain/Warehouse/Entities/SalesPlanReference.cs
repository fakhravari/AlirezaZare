namespace Shop.Database.Domain.Warehouse.Entities;

public sealed class SalesPlanReference
{
    public long Id { get; set; }
    public string StoreSalesPlanKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsRegular { get; set; }
    public bool IsActive { get; set; }
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
}