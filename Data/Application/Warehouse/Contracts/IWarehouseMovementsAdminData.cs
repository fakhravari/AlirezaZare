using Shop.Data.Application.Admin.Contracts;
using Shop.Data.Helpers;

namespace Shop.Data.Application.Warehouse.Contracts;

public interface IWarehouseMovementsAdminData
{
    Task<AdminOperationResult> Get(bool products, long? warehouseId, string? search,
        DateOnly? from, DateOnly? to, int page = 1,
        int pageSize = AdminListQuery.DefaultPageSize,
        bool audit = false, bool summary = false, string? salesPlan = null,
        string? direction = null, long? productId = null,
        CancellationToken cancellationToken = default);
}
