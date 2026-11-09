namespace Shop.Database.Application.Warehouse.Models;

public sealed record WarehouseStockItem(
    long IdWarehouse,
    long IdInventoryItem,
    long IdSalesPlanReference,
    string WarehouseCode,
    string WarehouseName,
    long IdProduct,
    string StoreProductKey,
    string ProductName,
    long? IdProductVariant,
    string? VariantName,
    string SalesPlanKey,
    string SalesPlanName,
    bool IsRegularSalesPlan,
    decimal Quantity,
    decimal MinimumQuantity,
    bool IsLowStock);
