namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreSchemaMigration
{
    public long Id { get; set; }
    public string MigrationKey { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; }
}