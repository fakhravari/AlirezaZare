namespace Shop.Data.Application.Store.Contracts;

public interface IStorePriceVersionService
{
    Task<long> AppendAsync(long productId, long salesPlanId, long? productVariantId, long? optionValueId, long? accessoryId, decimal amount, decimal discountPercent, string? changedBy, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(long productId, long salesPlanId, long? productVariantId, long? optionValueId, long? accessoryId, CancellationToken cancellationToken = default);
}
