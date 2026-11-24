namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreProductSalesPlanOffer
{
    public decimal Id { get; set; }
    public decimal IdProduct { get; set; }
    public long IdSalesPlan { get; set; }
    public int MaxPurchaseQuantity { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class StoreProductVariantSalesPlanOffer
{
    public decimal Id { get; set; }
    public long IdVariant { get; set; }
    public long IdSalesPlan { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class StoreAccessory
{
    public decimal Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public sealed class StoreProductAccessory
{
    public decimal Id { get; set; }
    public decimal IdProduct { get; set; }
    public decimal IdAccessory { get; set; }
    public bool IsRequired { get; set; }
    public decimal? DisplayDiscountPercent { get; set; }
    public int SortOrder { get; set; }
}

public sealed class StoreProductSalesPlanOfferAccessory
{
    public decimal Id { get; set; }
    public decimal IdOffer { get; set; }
    public decimal IdAccessory { get; set; }
    public bool IsIncludedByDefault { get; set; }
    public bool IsAllowed { get; set; }
}

public sealed class StoreProductSalesPlanOfferSpecificationOverride
{
    public long Id { get; set; }
    public decimal IdOffer { get; set; }
    public string SpecificationKey { get; set; } = "";
    public string Value { get; set; } = "";
}
