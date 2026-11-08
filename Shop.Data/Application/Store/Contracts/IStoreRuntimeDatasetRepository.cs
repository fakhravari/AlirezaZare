namespace Shop.Database.Application.Store.Contracts;

public interface IStoreRuntimeDatasetRepository
{
    Task<string?> ReadJsonAsync(string datasetKey, CancellationToken cancellationToken);
    Task<string?> ReadAccountJsonAsync(CancellationToken cancellationToken);
    Task<string?> ReadMagazineJsonAsync(CancellationToken cancellationToken);
    Task<string> ReadGeographyJsonAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<StoreFaqRow>> ReadFaqAsync(CancellationToken cancellationToken);
}

public sealed record StoreFaqRow(string Question, string Answer, string? LinkText, string? LinkUrl, int SortOrder);
