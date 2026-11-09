namespace Shop.Database.Application.Warehouse.Models;

public sealed record WarehouseInfo(
    long Id,
    string Code,
    string Name,
    string CompanyName,
    bool IsSalesWarehouse,
    bool IsActive);