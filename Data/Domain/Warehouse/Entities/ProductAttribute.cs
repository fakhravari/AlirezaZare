namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class ProductAttribute
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public WarehouseProduct Product { get; set; } = null!;
    public ICollection<ProductAttributeValue> Values { get; set; } = new List<ProductAttributeValue>();
}
