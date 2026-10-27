namespace Shop.Services;

/// <summary>Validated catalog snapshot shared by pages, SEO and browser bootstrap.</summary>
public interface ICatalogSource
{
    ValueTask<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken = default);
}
