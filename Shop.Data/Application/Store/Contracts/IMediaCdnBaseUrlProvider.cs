namespace Shop.Database.Application.Store.Contracts;

public interface IMediaCdnBaseUrlProvider
{
    Task<string> GetBaseUrlAsync(CancellationToken cancellationToken = default);
}
