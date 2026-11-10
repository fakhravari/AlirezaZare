namespace Shop.Data.Application.Store.Contracts;

public interface IStoreRuntimeDatasetRepository
{
    Task<string?> ReadCatalogJsonAsync(CancellationToken cancellationToken);
    Task<string?> ReadBrandEditorialJsonAsync(CancellationToken cancellationToken);
    Task<string?> ReadAccountJsonAsync(CancellationToken cancellationToken);
    Task<string?> ReadMagazineJsonAsync(CancellationToken cancellationToken);
    Task<string> ReadGeographyJsonAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<StoreFaqRow>> ReadFaqAsync(CancellationToken cancellationToken);
}

public sealed record StoreFaqRow(string Question, string Answer, string? LinkText, string? LinkUrl, int SortOrder);