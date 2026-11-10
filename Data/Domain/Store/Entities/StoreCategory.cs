namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreCategory
{
    public long Id { get; set; }
    public string CategoryKey { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameFa { get; set; } = string.Empty;
    public string PathJson { get; set; } = "[]";
    public long? IdParentCategory { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
