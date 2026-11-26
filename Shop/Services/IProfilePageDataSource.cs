using Shop.Models;

namespace Shop.Services;

// A future authenticated implementation must resolve the current user server-side.
public interface IProfilePageDataSource
{
    Task<AccountData> ReadAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseProfilePageDataSource(
    Shop.Data.Application.Store.Contracts.IStoreRuntimeDatasetRepository datasets)
    : IProfilePageDataSource
{
    private Task<AccountData>? _requestSnapshot;

    public Task<AccountData> ReadAsync(CancellationToken cancellationToken = default) =>
        _requestSnapshot ??= LoadAsync(cancellationToken);

    private async Task<AccountData> LoadAsync(CancellationToken cancellationToken)
    {
        var json = await datasets.ReadAccountJsonAsync(cancellationToken) ??
                   throw new InvalidDataException("The account dataset is missing from the database.");

        return AccountData.Parse(json);
    }
}
