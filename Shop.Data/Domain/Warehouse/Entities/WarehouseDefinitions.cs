namespace Shop.Database.Domain.Warehouse.Entities;

public sealed class WarehouseDocumentType
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Direction { get; set; } = "R";
    public bool AffectsStock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class WarehouseUnit
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class WarehouseParty
{
    public long Id { get; set; }
    public long? IdWarehouse { get; set; }
    public string PartyType { get; set; } = "Other";
    public string? ExternalPartyKey { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class WarehouseProductUnit
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public long IdUnit { get; set; }
    public decimal ConversionFactor { get; set; }
    public bool IsBaseUnit { get; set; }
    public bool IsActive { get; set; }
}
