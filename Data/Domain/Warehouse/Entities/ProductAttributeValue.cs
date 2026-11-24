namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class ProductAttributeValue
{
    public long Id { get; set; }
    public decimal IdProduct { get; set; }
    public long IdProductAttribute { get; set; }
    public string ValueKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public string? Swatch { get; set; }
    public bool IsActive { get; set; } = true;
    public ProductAttribute Attribute { get; set; } = null!;
    public ICollection<ProductVariantAttributeValue> Variants { get; set; } = new List<ProductVariantAttributeValue>();
}