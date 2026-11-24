using Shop.Data.Domain.Store.Entities;

namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class Voucher
{
    public decimal Id { get; set; }
    public long IdWarehouse { get; set; }
    public long IdVoucherType { get; set; }
    public long IdParty { get; set; }
    public long IdSalesPlanReference { get; set; }
    public long VoucherNumber { get; set; }
    public DateOnly VoucherDate { get; set; }
    public int IdVoucherStatus { get; set; }
    public VoucherStatus StatusReference { get; set; } = null!;
    public string? CustomerOrderKey { get; set; }
    public string? ExternalReference { get; set; }
    public string? Reason { get; set; }
    public string CreatedByUserKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedByUserKey { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<VoucherLine> Lines { get; set; } = [];
}

public sealed class VoucherLine
{
    public long Id { get; set; }
    public decimal IdVoucher { get; set; }
    public decimal IdInventoryItem { get; set; }
    public long IdUnit { get; set; }
    public long? IdSalesPlanReference { get; set; }
    public decimal Quantity { get; set; }
    public decimal ConversionFactor { get; set; }
    public decimal BaseQuantity { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string? VariantNameSnapshot { get; set; }
    public string UnitNameSnapshot { get; set; } = string.Empty;
}

public sealed class WarehouseStockMovement
{
    public decimal Id { get; set; }
    public long IdWarehouse { get; set; }
    public decimal IdInventoryItem { get; set; }
    public long IdSalesPlanReference { get; set; }
    public decimal IdVoucher { get; set; }
    public long IdVoucherLine { get; set; }
    public decimal QuantityDelta { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}

public sealed class VoucherAudit
{
    public long Id { get; set; }
    public long VoucherId { get; set; }
    public long VoucherNumber { get; set; }
    public long WarehouseId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorUserKey { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? Reason { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}