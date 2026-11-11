namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreProductSalesPlanOffer
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public long IdSalesPlan { get; set; }
    public decimal PriceAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public int MaxPurchaseQuantity { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class StoreProductVariantSalesPlanOffer
{
    public long Id { get; set; }
    public long IdProductVariant { get; set; }
    public long IdSalesPlan { get; set; }
    public decimal PriceAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class StoreAccessory
{
    public long Id { get; set; }
    public string AccessoryKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public sealed class StoreProductAccessory
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public long IdAccessory { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public decimal PriceAmount { get; set; }
}

public sealed class StoreProductSalesPlanOfferAccessory
{
    public long Id { get; set; }
    public long IdProductSalesPlanOffer { get; set; }
    public long IdAccessory { get; set; }
    public decimal PriceAmount { get; set; }
    public bool IsIncludedByDefault { get; set; }
    public bool IsAllowed { get; set; }
}

public sealed class StoreProductSalesPlanOfferSpecificationOverride
{
    public long Id { get; set; }
    public long IdProductSalesPlanOffer { get; set; }
    public string SpecificationKey { get; set; } = "";
    public string Value { get; set; } = "";
}
