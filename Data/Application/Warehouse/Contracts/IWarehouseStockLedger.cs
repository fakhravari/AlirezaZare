namespace Shop.Data.Application.Warehouse.Contracts;

public interface IWarehouseStockLedger
{
    Task RebuildVoucherAsync(decimal voucherId, CancellationToken cancellationToken);
    Task ReverseMovementsAsync(decimal voucherId, CancellationToken cancellationToken);
}

public sealed class InsufficientWarehouseStockException : Exception
{
}
