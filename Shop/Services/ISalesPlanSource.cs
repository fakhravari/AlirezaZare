using Shop.Models;

namespace Shop.Services;

public interface ISalesPlanSource
{
    ValueTask<SalesPlanCatalog> ReadAsync(CancellationToken cancellationToken = default);
}

/// <summary>Read-only local fixture adapter; replace this implementation with a database/API adapter later.</summary>
public sealed class DemoSalesPlanSource(
    IWebHostEnvironment environment,
    ICatalogSource catalogSource) : ISalesPlanSource
{
    private readonly string _path = Path.Combine(environment.ContentRootPath, "App_Data", "sales-plans-demo.json");
    private readonly object _gate = new();
    private DateTime _lastWriteUtc;
    private string? _catalogJson;
    private SalesPlanCatalog? _snapshot;

    public async ValueTask<SalesPlanCatalog> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CatalogSnapshot catalog = await catalogSource.ReadAsync(cancellationToken);
        lock (_gate)
        {
            FileInfo file = new(_path);
            if (_snapshot is not null && file.LastWriteTimeUtc == _lastWriteUtc &&
                StringComparer.Ordinal.Equals(_catalogJson, catalog.BrowserJson))
                return _snapshot;

            SalesPlanCatalog parsed = SalesPlanCatalog.ParseConfiguration(
                File.ReadAllText(_path), catalog.BrowserJson);
            _snapshot = parsed;
            _lastWriteUtc = file.LastWriteTimeUtc;
            _catalogJson = catalog.BrowserJson;
            return parsed;
        }
    }
}
