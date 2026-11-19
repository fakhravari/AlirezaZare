namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class ProductVariantAttributeValue
{
    public long IdProductVariant { get; set; }
    public long IdProduct { get; set; }
    public long IdProductAttribute { get; set; }
    public long IdProductAttributeValue { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public ProductAttributeValue AttributeValue { get; set; } = null!;
}
