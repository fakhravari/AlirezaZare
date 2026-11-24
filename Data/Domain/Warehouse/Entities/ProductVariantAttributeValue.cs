namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class ProductVariantAttributeValue
{
    public long IdProductVariant { get; set; }
    public decimal IdProduct { get; set; }
    public long IdProductAttribute { get; set; }
    public long IdProductAttributeValue { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public ProductAttributeValue AttributeValue { get; set; } = null!;
}