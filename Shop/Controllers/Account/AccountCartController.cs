using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Account.Contracts;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;
using Shop.Services;
using Shop.Data.Application.Store.Models;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;

namespace Shop.Controllers.Account;

[ApiController]
[Authorize(AuthenticationSchemes = "ShopCookie")]
[Route("api/account")]
public sealed class AccountCartController : AccountApiControllerBase
{
    private readonly ICheckoutSalesPlans plans;
    private readonly IProductPurchaseConfigurationSource purchaseConfigurations;
    private readonly IAccountDataCommands data;

    public AccountCartController(ICheckoutSalesPlans salesPlans, IProductPurchaseConfigurationSource configurations,
        IAccountDataCommands accountData) : base(accountData)
    {
        plans = salesPlans;
        purchaseConfigurations = configurations;
        data = accountData;
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
                !await data.ProductVariantExistsAsync(item.ProductId, item.VariantId.Value, ct))
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

            var productAccessories = await data.ReadCartAccessoriesAsync(item.ProductId, ct);
            var selectedAccessories = productAccessories
                .Where(accessory => accessoryIds.Contains(accessory.ProductAccessoryId)).ToArray();
            ProductSalesOffer? cartOffer = currentCatalog.Offers.SingleOrDefault(offer =>
                offer.ProductId == item.ProductId && offer.SalesPlanId == item.SalesPlanId);
            if (cartOffer is null || selectedAccessories.Length != accessoryIds.Length ||
                productAccessories.Any(accessory => accessory.Required &&
                    !accessoryIds.Contains(accessory.ProductAccessoryId)) ||
                selectedAccessories.Any(accessory => cartOffer.AccessoryIds is { } allowed &&
                                                     !allowed.Contains(
                                                         accessory.AccessoryId.ToString(System.Globalization.CultureInfo
                    .InvariantCulture), StringComparer.Ordinal)) ||
                item.Quantity > configuration.MaxPurchaseQuantity)
                return Conflict(new { code = "invalid-product-options" });
        }

        var cartLines = request.Items.Select(line => new AccountCartLineData(line.ProductId, line.SalesPlanId,
            line.VariantId, line.Quantity, line.AccessoryIds?.ToArray() ?? [])).ToArray();
        if (!await data.SaveCartAsync(ProfileId, cartLines, ct))
            return Conflict(new { code = "product-or-plan-unavailable" });

        return Ok(new { saved = true });
    }
}
