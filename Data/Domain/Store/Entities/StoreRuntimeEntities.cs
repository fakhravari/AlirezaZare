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
    public string? CountryName { get; set; }
    public string? FullPastedText { get; set; }
    public string? FullDocumentText { get; set; }
}

public sealed class StoreSpecificationGroup
{
    public long Id { get; set; }
    public string GroupKey { get; set; } = "";
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class StoreSpecificationCategory
{
    public long Id { get; set; }
    public long IdSpecificationGroup { get; set; }
    public string CategoryKey { get; set; } = "";
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class StoreSpecificationFeature
{
    public long Id { get; set; }
    public long IdSpecificationCategory { get; set; }
    public string FeatureKey { get; set; } = "";
    public string Label { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class StoreProductSpecificationValue
{
    public decimal Id { get; set; }
    public decimal IdProduct { get; set; }
    public long IdSpecificationFeature { get; set; }
    public string Value { get; set; } = "";
}

public sealed class StoreProductCollection
{
    public long Id { get; set; }
    public string CollectionKey { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class StoreProductCollectionItem
{
    public decimal Id { get; set; }
    public long IdCollection { get; set; }
    public decimal IdProduct { get; set; }
    public int SortOrder { get; set; }
}

public sealed class StoreHomeCategoryTab
{
    public long Id { get; set; }
    public long IdCategory { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
