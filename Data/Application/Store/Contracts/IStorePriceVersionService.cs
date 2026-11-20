namespace Shop.Data.Application.Store.Contracts;

public interface IStorePriceVersionService
{
    Task<long> AppendAsync(long productId, long salesPlanId, long? productVariantId, long? accessoryId, decimal amount, decimal discountPercent, long idChangedBy, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(long productId, long salesPlanId, long? productVariantId, long? accessoryId, CancellationToken cancellationToken = default);
}
