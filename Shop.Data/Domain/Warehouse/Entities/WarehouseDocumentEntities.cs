namespace Shop.Database.Domain.Warehouse.Entities;

public sealed class WarehouseDocumentRecord
{
    public long Id { get; set; }
    public long IdWarehouse { get; set; }
    public long IdDocumentType { get; set; }
    public long IdParty { get; set; }
    public long IdSalesPlanReference { get; set; }
    public long DocumentNumber { get; set; }
    public DateOnly DocumentDate { get; set; }
    public string Status { get; set; } = "D";
    public string? CustomerOrderKey { get; set; }
    public string? ExternalReference { get; set; }
    public string? Reason { get; set; }
    public string CreatedByUserKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedByUserKey { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<WarehouseDocumentLineRecord> Lines { get; set; } = [];
}

public sealed class WarehouseDocumentLineRecord
{
    public long Id { get; set; }
    public long IdDocument { get; set; }
    public long IdInventoryItem { get; set; }
    public long IdUnit { get; set; }
    public decimal Quantity { get; set; }
    public decimal ConversionFactor { get; set; }
    public decimal BaseQuantity { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string? VariantNameSnapshot { get; set; }
    public string UnitNameSnapshot { get; set; } = string.Empty;
}

public sealed class WarehouseStockMovement
{
    public long Id { get; set; }
    public long IdWarehouse { get; set; }
    public long IdInventoryItem { get; set; }
    public long IdSalesPlanReference { get; set; }
    public long IdDocument { get; set; }
    public long IdDocumentLine { get; set; }
    public decimal QuantityDelta { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}

public sealed class WarehouseDocumentAudit
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public long DocumentNumber { get; set; }
    public long WarehouseId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorUserKey { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? Reason { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}
