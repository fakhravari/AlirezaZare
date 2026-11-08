namespace Shop.Database.Domain.Warehouse.Entities;

public sealed class ProductVariant
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public WarehouseProduct Product { get; set; } = null!;
}
