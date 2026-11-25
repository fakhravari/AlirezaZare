namespace Shop.Data.Application.Account.Contracts;

public interface IAccountStateQueryService
{
    Task<string?> ReadJsonAsync(long profileId, CancellationToken cancellationToken = default);
}