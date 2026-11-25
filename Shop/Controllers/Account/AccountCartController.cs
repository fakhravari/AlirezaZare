using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Application.Store;
using Shop.Services;
using Shop.Models;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;

namespace Shop.Controllers.Account;

[ApiController]
[Authorize(AuthenticationSchemes = "ShopCookie")]
[Route("api/account")]
public sealed class AccountCartController : AccountApiControllerBase
{
    private readonly ISalesPlanSource plans;
    private readonly IProductPurchaseConfigurationSource purchaseConfigurations;

    public AccountCartController(ShopDbContext context, ISalesPlanSource salesPlans, IProductPurchaseConfigurationSource configurations) : base(context)
    {
        plans = salesPlans;
        purchaseConfigurations = configurations;
    }
    [HttpPut("cart")]
    public async Task<IActionResult> SaveCart([FromBody] CartRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count > 99 || request.Items.Any(i =>
                i is null || i.ProductId is < 1 or > int.MaxValue || i.VariantId is < 1 || i.Quantity is < 1 or > 99 ||
                string.IsNullOrWhiteSpace(i.SalesPlanId) || i.SalesPlanId.Length > 64))
            return BadRequest(new { code = "invalid-cart" });

        var duplicate = request.Items.GroupBy(i => new
        {
            i.ProductId,
            i.SalesPlanId
        }).Any(g => g.Count() > 1);
        if (duplicate) return BadRequest(new { code = "duplicate-cart-line" });

        SalesPlanCatalog currentCatalog = await plans.ReadAsync(ct);
        foreach (var item in request.Items)
        {
            try
            {
                // Recheck the offer's current plan, quantity limit, and product-level stock
                // before accepting a cart snapshot. Variant stock is checked separately below.
                currentCatalog.Quote(checked((int)item.ProductId), item.SalesPlanId, item.Quantity,
                    DateTimeOffset.UtcNow);
            }
            catch (SalesPlanUnavailableException exception)
            {
                return Conflict(new { code = exception.ReasonCode });
            }

            ProductInventoryVariant[] variants = currentCatalog.InventoryVariants
                .Where(variant => variant.ProductId == item.ProductId).ToArray();
            if (variants.Length > 0 && (!item.VariantId.HasValue ||
                                        variants.All(variant => variant.VariantId != item.VariantId.Value)))
                return Conflict(new { code = "inventory-variant-required" });

            if (variants.Length == 0 && item.VariantId.HasValue || item.VariantId.HasValue &&
                !await ProductVariantExistsAsync(item.ProductId, item.VariantId.Value, ct))
                return Conflict(new { code = "product-variant-unavailable" });

            if (item.VariantId is { } selectedVariantId &&
                (!variants.Single(variant => variant.VariantId == selectedVariantId).StockBySalesPlan
                    .TryGetValue(item.SalesPlanId, out decimal stock) || stock < item.Quantity))
                return Conflict(new { code = "insufficient-stock" });
            if (item.VariantId is { } pricedVariantId &&
                !variants.Single(variant => variant.VariantId == pricedVariantId).PriceBySalesPlan
                    .ContainsKey(item.SalesPlanId))
                return Conflict(new { code = "variant-price-unavailable" });

            ProductPurchaseConfiguration? configuration =
                await purchaseConfigurations.ReadAsync(checked((int)item.ProductId), item.SalesPlanId, ct);
            if (configuration is null) return Conflict(new { code = "product-options-unavailable" });

            decimal[] accessoryIds = item.AccessoryIds?.ToArray() ?? [];
            if (accessoryIds.Distinct().Count() != accessoryIds.Length || accessoryIds.Any(id => id < 1))
                return BadRequest(new { code = "invalid-product-options" });

            var productAccessories = await db.StoreProductAccessories
                .AsNoTracking()
                .Join(
                    db.StoreAccessories
                        .AsNoTracking(),
                    accessory => accessory.IdAccessory,
                    definition => definition.Id,
                    (accessory, definition) => new { accessory, definition })
                .Where(accessoryDefinition =>
                    accessoryDefinition.accessory.IdProduct == item.ProductId && accessoryDefinition.definition.IsActive)
                .Select(accessoryDefinition => new
                {
                    accessoryDefinition.accessory.Id,
                    accessoryDefinition.accessory.IsRequired,
                    AccessoryId = accessoryDefinition.definition.Id
                })
                .ToListAsync(ct);
            var selectedAccessories =
                productAccessories.Where(accessory => accessoryIds.Contains(accessory.Id)).ToArray();
            ProductSalesOffer? cartOffer = currentCatalog.Offers.SingleOrDefault(offer =>
                offer.ProductId == item.ProductId && offer.SalesPlanId == item.SalesPlanId);
            if (cartOffer is null || selectedAccessories.Length != accessoryIds.Length ||
                productAccessories.Any(accessory => accessory.IsRequired && !accessoryIds.Contains(accessory.Id)) ||
                selectedAccessories.Any(accessory => cartOffer.AccessoryIds is { } allowed &&
                                                     !allowed.Contains(
                                                         accessory.AccessoryId.ToString(System.Globalization.CultureInfo
                                                             .InvariantCulture), StringComparer.Ordinal)) ||
                item.Quantity > configuration.MaxPurchaseQuantity)
                return Conflict(new { code = "invalid-product-options" });
        }

        decimal[] productIds = request.Items.Select(x => x.ProductId).Distinct().ToArray();
        string[] planKeys = request.Items.Select(x => x.SalesPlanId).Distinct(StringComparer.Ordinal).ToArray();
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var availableProducts = await db.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id) && x.IsStoreEnabled)
            .Select(x => x.Id)
            .ToHashSetAsync(ct);
        var planIds = await db.SalesPlanReferences
            .AsNoTracking()
            .Where(x =>
                planKeys.Contains(x.StoreSalesPlanKey) && x.IsActive && x.IdStockOwnerType == 1)
            .ToDictionaryAsync(x => x.StoreSalesPlanKey, x => x.Id, ct);
        if (availableProducts.Count != productIds.Length || planIds.Count != planKeys.Length)
            return Conflict(new { code = "product-or-plan-unavailable" });

        var cart = await db.AccountCarts
            .SingleOrDefaultAsync(x => x.IdProfile == ProfileId, ct);
        if (cart is null)
        {
            cart = new AccountCart { IdProfile = ProfileId, UpdatedAtUtc = DateTime.UtcNow };
            db.AccountCarts.Add(cart);
            await db.SaveChangesAsync(ct);
        }

        await db.AccountCartItems
            .Where(x => x.IdCart == cart.Id)
            .ExecuteDeleteAsync(ct);
        DateTime now = DateTime.UtcNow;
        var cartItemEntities = request.Items.Select(item => new AccountCartItem
        {
            IdCart = cart.Id,
            IdProduct = item.ProductId,
            IdVariant = item.VariantId,
            IdSalesPlan = planIds[item.SalesPlanId],
            Quantity = item.Quantity,
            UpdatedAtUtc = now
        }).ToArray();
        db.AccountCartItems.AddRange(cartItemEntities);
        await db.SaveChangesAsync(ct);

        for (var index = 0; index < request.Items.Count; index++)
        {
            CartLineRequest line = request.Items[index];
            AccountCartItem savedItem = cartItemEntities[index];
            db.AccountCartItemAccessories.AddRange((line.AccessoryIds ?? []).Select(id => new AccountCartItemAccessory
                { IdCartItem = savedItem.Id, IdProductAccessory = id }));
        }

        cart.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Ok(new { saved = true });
    }
}
