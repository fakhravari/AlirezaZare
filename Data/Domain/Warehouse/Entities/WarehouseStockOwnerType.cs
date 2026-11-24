namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class WarehouseStockOwnerType
{
    public byte Id { get; set; }
    public string TypeKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class WarehouseSchemaMigration
{
    public long Id { get; set; }
    public string MigrationKey { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; }
}