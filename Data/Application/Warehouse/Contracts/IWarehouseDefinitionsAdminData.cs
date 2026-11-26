using Shop.Data.Application.Admin.Contracts;
using System.Text.Json;

namespace Shop.Data.Application.Warehouse.Contracts;

public interface IWarehouseDefinitionsAdminData
{
    Task<AdminOperationResult> Get(string section, CancellationToken cancellationToken);
    Task<AdminOperationResult> GetVariantNames(CancellationToken cancellationToken);
    Task<AdminOperationResult> CreateVariantName(VariantNameInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> UpdateVariantName(long id, VariantNameInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> ActivateVariantName(long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> DeleteUnusedVariantName(long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> GetVariantOptions(long productId, long? variantId,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> Create(string section, JsonElement payload,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> Save(string section, long id, JsonElement payload,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> DeleteUnusedUnitDefinition(string section, long id,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> DeleteUnusedVariant(long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> SaveMapping(SalesPlanMappingInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> RemoveMapping(long warehouseId, long ownerId, CancellationToken cancellationToken);
    Task<AdminOperationResult> SetDefaultMapping(long warehouseId, long ownerId,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> Deactivate(string section, long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> ActivateVariant(long id, CancellationToken cancellationToken);
}

public sealed record VariantNameInput(long? IdParent, string? Name);

public sealed record SalesPlanMappingInput(
    long CompanyId,
    long WarehouseId,
    long SalesPlanId,
    bool IsDefault = false);
