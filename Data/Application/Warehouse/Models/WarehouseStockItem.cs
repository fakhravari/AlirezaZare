namespace Shop.Data.Application.Warehouse.Models;

public sealed record WarehouseStockItem(
    long IdWarehouse,
    decimal IdInventoryItem,
    long IdSalesPlanReference,
    string WarehouseCode,
    string WarehouseName,
    string CompanyName,
    decimal IdProduct,
    string StoreProductKey,
    string ProductName,
    long? IdProductVariant,
    string? VariantName,
    string SalesPlanKey,
    string SalesPlanName,
    string UnitName,
    bool IsRegularSalesPlan,
    decimal Quantity,
    decimal MinimumQuantity,
    bool IsLowStock);
