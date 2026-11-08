namespace Shop.Database.Application.Warehouse.Models;

public sealed record WarehouseStockItem(
    long IdWarehouse,
    string WarehouseCode,
    string WarehouseName,
    long IdStoreProduct,
    string StoreProductKey,
    string ProductName,
    long? IdProductVariant,
    string? VariantName,
    string SalesPlanKey,
    string SalesPlanName,
    decimal Quantity,
    decimal MinimumQuantity,
    bool IsLowStock);
