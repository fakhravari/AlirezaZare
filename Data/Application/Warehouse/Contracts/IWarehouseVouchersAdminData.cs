using Shop.Data.Application.Admin.Contracts;
using Shop.Data.Helpers;

namespace Shop.Data.Application.Warehouse.Contracts;

public interface IWarehouseVouchersAdminData
{
    Task<AdminOperationResult> List(long? warehouseId, string? status,
        string? q,
        int page = 1, int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default);
    Task<AdminOperationResult> Lookups(CancellationToken cancellationToken);
    Task<AdminOperationResult> Detail(decimal id, CancellationToken cancellationToken);
    Task<AdminOperationResult> Create(SaveVoucherInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> Update(decimal id, SaveVoucherInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> Delete(decimal id, CancellationToken cancellationToken);
}

public sealed record SaveVoucherInput(
    long WarehouseId,
    long VoucherTypeId,
    long PartyId,
    long SalesPlanId,
    DateOnly VoucherDate,
    string Status,
    string Reason,
    string? ExternalReference,
    IReadOnlyList<VoucherLineInput>? Lines);

public sealed record VoucherLineInput(decimal InventoryItemId, long UnitId, decimal Quantity, long? SalesPlanId);
