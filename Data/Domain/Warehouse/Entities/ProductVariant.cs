namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class ProductVariant
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AttributesJson { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public WarehouseProduct Product { get; set; } = null!;
}
