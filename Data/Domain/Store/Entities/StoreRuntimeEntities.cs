namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreBrandEditorial
{
    public long Id { get; set; }
    public long IdBrand { get; set; }
    public string? Established { get; set; }
    public string? OfficialUrl { get; set; }
    public string? Specialization { get; set; }
    public string? FeaturedProducts { get; set; }
    public string? Audience { get; set; }
    public string? ShoppingAdvice { get; set; }
    public string? CompanyIntroductionsJson { get; set; }
    public string? ProductFamiliesJson { get; set; }
    public string? StrengthsJson { get; set; }
}

public sealed class StoreColor { public long Id { get; set; } public string ColorKey { get; set; } = ""; public string NameEn { get; set; } = ""; public string NameFa { get; set; } = ""; public string? HexCode { get; set; } }
public sealed class StoreSpecificationGroup { public long Id { get; set; } public string GroupKey { get; set; } = ""; public string Name { get; set; } = ""; public int SortOrder { get; set; } }
public sealed class StoreSpecificationCategory { public long Id { get; set; } public long IdSpecificationGroup { get; set; } public string CategoryKey { get; set; } = ""; public string Name { get; set; } = ""; public int SortOrder { get; set; } }
public sealed class StoreSpecificationFeature { public long Id { get; set; } public long IdSpecificationCategory { get; set; } public string FeatureKey { get; set; } = ""; public string Label { get; set; } = ""; public int SortOrder { get; set; } }
public sealed class StoreProductColor { public long Id { get; set; } public long IdProduct { get; set; } public long IdColor { get; set; } }
public sealed class StoreProductSpecificationValue { public long Id { get; set; } public long IdProduct { get; set; } public long IdSpecificationFeature { get; set; } public string Value { get; set; } = ""; }
public sealed class StoreProductCollection { public long Id { get; set; } public string CollectionKey { get; set; } = ""; public string Name { get; set; } = ""; }
public sealed class StoreProductCollectionItem { public long Id { get; set; } public long IdProductCollection { get; set; } public long IdProduct { get; set; } public int SortOrder { get; set; } }
