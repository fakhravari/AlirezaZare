using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Controllers.Account;

public abstract class AccountApiControllerBase(ShopDbContext context) : ControllerBase
{
    protected readonly ShopDbContext db = context;

    protected long ProfileId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    protected Task<bool> ProductVariantExistsAsync(decimal productId, long variantId,
        CancellationToken cancellationToken) =>
        db.ProductVariants
            .AsNoTracking()
            .AnyAsync(variant =>
                variant.Id == variantId && variant.IdProduct == productId && variant.IsActive &&
                variant.Product.IsStoreEnabled,
                cancellationToken);
}
