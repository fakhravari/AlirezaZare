namespace Shop.Data.Application.Store.Contracts;

public interface IStorePriceVersionService
{
    Task<decimal> AppendAsync(decimal productId, long salesPlanId, long? productVariantId, decimal? accessoryId, decimal amount,
        decimal discountPercent, long idChangedBy, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(decimal productId, long salesPlanId, long? productVariantId, decimal? accessoryId,
        CancellationToken cancellationToken = default);
}
