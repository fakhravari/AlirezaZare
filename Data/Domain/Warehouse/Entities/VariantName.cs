namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class VariantName
{
    public long Id { get; set; }
    public long? IdParent { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public VariantName? Parent { get; set; }
}
