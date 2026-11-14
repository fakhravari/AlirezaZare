namespace Shop.Data.Application.Warehouse.Contracts;

public interface IWarehouseStockLedger
{
    Task RebuildDocumentAsync(long documentId, CancellationToken cancellationToken);
    Task ReverseMovementsAsync(long documentId, CancellationToken cancellationToken);
}

public sealed class InsufficientWarehouseStockException : Exception { }
