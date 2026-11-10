namespace Shop.Database.Domain.Store.Entities;

public sealed class StoreAttributeDefinition
{
    public long Id { get; set; }
    public string AttributeKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? SourceKey { get; set; }
    public string? FilterKey { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsHighlight { get; set; }
    public bool IsActive { get; set; } = true;
}
