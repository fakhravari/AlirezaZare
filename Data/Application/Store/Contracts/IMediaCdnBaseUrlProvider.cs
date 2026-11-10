namespace Shop.Data.Application.Store.Contracts;

public interface IMediaCdnBaseUrlProvider
{
    Task<string> GetBaseUrlAsync(CancellationToken cancellationToken = default);
}
