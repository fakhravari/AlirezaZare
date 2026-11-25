namespace Shop.Data.Application.Warehouse.Contracts;

public interface IVoucherNumberGenerator
{
    Task<long> NextAsync(CancellationToken cancellationToken);
}
