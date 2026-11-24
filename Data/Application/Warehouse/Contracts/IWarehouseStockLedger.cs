namespace Shop.Data.Application.Warehouse.Contracts;

public interface IWarehouseStockLedger
{
    Task RebuildVoucherAsync(long voucherId, CancellationToken cancellationToken);
    Task ReverseMovementsAsync(long voucherId, CancellationToken cancellationToken);
}

public sealed class InsufficientWarehouseStockException : Exception { }
