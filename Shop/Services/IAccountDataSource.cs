using Shop.Models;

namespace Shop.Services;

// A future authenticated implementation must resolve the current user server-side.
public interface IAccountDataSource
{
    Task<AccountData> ReadAsync(CancellationToken cancellationToken = default);
}

// Public offers deliberately expose no order history or customer data.
public interface ICommerceOfferSource
{
    Task<IReadOnlyDictionary<string, int>> ReadOffersAsync(DateOnly today,
        CancellationToken cancellationToken = default);
}

public sealed class DatabaseAccountDataSource(
    Shop.Database.Application.Store.Contracts.IStoreRuntimeDatasetRepository datasets)
    : IAccountDataSource, ICommerceOfferSource
{
    private Task<AccountData>? _requestSnapshot;

    public Task<AccountData> ReadAsync(CancellationToken cancellationToken = default) =>
        _requestSnapshot ??= LoadAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, int>> ReadOffersAsync(DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(cancellationToken);
        return data.Coupons.Where(c => c.IsActive(today)).ToDictionary(c => c.Code, c => c.DiscountPercent);
    }

    private async Task<AccountData> LoadAsync(CancellationToken cancellationToken)
    {
        var json = await datasets.ReadAccountJsonAsync(cancellationToken)
                   ?? throw new InvalidDataException("The account dataset is missing from the database.");
        return AccountData.Parse(json);
    }
}