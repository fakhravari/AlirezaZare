namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreCategory
{
    public long Id { get; set; }
    public string CategoryKey { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameFa { get; set; } = string.Empty;
    public string PathJson { get; set; } = "[]";
    public string TreePathIds { get; set; } = string.Empty;
    public string TreePathNames { get; set; } = string.Empty;
    public long? IdParentCategory { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ProductIntro { get; set; }
    public string? ProductHighlightsJson { get; set; }
    public string? ProductSpecsJson { get; set; }
    public string IconClass { get; set; } = "bi-grid-3x3-gap";
}