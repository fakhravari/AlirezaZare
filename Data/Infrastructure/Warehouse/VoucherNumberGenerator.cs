using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Warehouse;

public sealed class VoucherNumberGenerator(ShopDbContext db) : IVoucherNumberGenerator
{
    public Task<long> NextAsync(CancellationToken cancellationToken) => db.Database
        .SqlQueryRaw<long>("SELECT NEXT VALUE FOR [Warehouse].[VoucherNumberSequence] AS [Value]")
        .SingleAsync(cancellationToken);
}
