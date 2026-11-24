namespace Shop.Data.Domain.Store.Entities;

public static class StoreCatalogFeatureKinds
{
    public const byte PriceVariant = 1;
    public const byte Display = 2;
}

public sealed class StoreCatalogFeature
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public byte Kind { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StoreCatalogFeatureValue
{
    public long Id { get; set; }
    public long IdFeature { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class StoreCategoryCatalogFeature
{
    public long IdCategory { get; set; }
    public long IdFeature { get; set; }
}

public sealed class StoreProductCatalogFeatureValue
{
    public decimal IdProduct { get; set; }
    public long IdFeature { get; set; }
    public long IdFeatureValue { get; set; }
    public byte Kind { get; set; } = StoreCatalogFeatureKinds.Display;
}

public sealed class StoreVariantCatalogFeatureValue
{
    public decimal Id { get; set; }
    public long IdProductVariant { get; set; }
    public long IdFeature { get; set; }
    public long IdFeatureValue { get; set; }
    public byte Kind { get; set; } = StoreCatalogFeatureKinds.PriceVariant;
}