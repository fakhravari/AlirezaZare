using Microsoft.AspNetCore.Mvc;
using Shop.Models;
using Shop.Services;

namespace Shop.Controllers;

[ApiController]
[Route("api/store/sales-plans")]
public sealed class SalesPlansController(
    ISalesPlanSource plans,
    IProductPurchaseConfigurationSource purchaseConfigurations) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        Ok(await plans.ReadAsync(cancellationToken));

    [HttpGet("products/{productId:int}")]
    public async Task<IActionResult> Product(int productId, CancellationToken cancellationToken)
    {
        SalesPlanCatalog data = await plans.ReadAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var productOffers = data.Offers.Where(offer => offer.ProductId == productId).ToArray();
        if (productOffers.Length == 0) return NotFound(new { code = "product-not-found" });
        return Ok(new
        {
            productId,
            currency = data.Currency,
            inventoryVariants = data.InventoryVariants.Where(variant => variant.ProductId == productId),
            priceOptions = data.PriceOptions.GetValueOrDefault(productId, []),
            plans = data.Plans.Select(plan => new
            {
                plan.Id, plan.Code, plan.Name, plan.Type, plan.IsDefault, plan.Label,
                plan.StartsAtUtc, plan.EndsAtUtc, plan.ShowCountdown,
                active = plan.IsEnabled && (plan.StartsAtUtc is null || now >= plan.StartsAtUtc) &&
                         (plan.EndsAtUtc is null || now < plan.EndsAtUtc),
                offer = productOffers.FirstOrDefault(offer => offer.SalesPlanId == plan.Id)
            })
        });
    }

    [HttpPost("quote")]
    public async Task<IActionResult> Quote([FromBody] QuoteRequest request, CancellationToken cancellationToken)
    {
        if (request.ProductId < 1 || request.Quantity is < 1 or > 99)
            return BadRequest(new { code = "invalid-request" });
        try
        {
            SalesPlanCatalog data = await plans.ReadAsync(cancellationToken);
            SalesPlanQuote quote = data.Quote(request.ProductId, request.SalesPlanId,
                request.Quantity, DateTimeOffset.UtcNow);
            string selectedPlanId = quote.SalesPlanId;
            ProductInventoryVariant[] productInventoryVariants = data.InventoryVariants
                .Where(variant => variant.ProductId == request.ProductId).ToArray();
            if (productInventoryVariants.Length > 0)
            {
                ProductInventoryVariant? inventoryVariant = request.VariantId is { } variantId
                    ? productInventoryVariants.SingleOrDefault(variant => variant.VariantId == variantId)
                    : null;
                if (inventoryVariant is null) throw new SalesPlanUnavailableException("inventory-variant-required");
                inventoryVariant.StockBySalesPlan.TryGetValue(selectedPlanId, out decimal variantStock);
                if (variantStock < request.Quantity) throw new SalesPlanUnavailableException("insufficient-stock");
            }
            else if (request.VariantId.HasValue)
                throw new SalesPlanUnavailableException("product-variant-unavailable");
            ProductSalesOffer selectedOffer = data.Offers.Single(item =>
                item.ProductId == quote.ProductId && item.SalesPlanId == quote.SalesPlanId);
            (decimal adjustments, IReadOnlyDictionary<string, decimal> accessoryAmounts, int maxQuantity) =
                await GetValidatedAdjustments(request, selectedOffer, cancellationToken);
            if (request.Quantity > maxQuantity)
                throw new SalesPlanUnavailableException("purchase-limit");
            decimal listUnitAmount = quote.ListUnitAmount;
            decimal discountPercent = quote.DiscountPercent;
            decimal baseUnitAmount = quote.UnitAmount;
            ProductInventoryVariant? selectedInventoryVariant = data.InventoryVariants.SingleOrDefault(variant =>
                variant.ProductId == request.ProductId && variant.VariantId == request.VariantId);
            if (selectedInventoryVariant is not null && selectedInventoryVariant.PriceBySalesPlan.TryGetValue(selectedPlanId, out decimal variantPrice))
            {
                listUnitAmount = variantPrice;
                discountPercent = selectedInventoryVariant.DiscountBySalesPlan.GetValueOrDefault(selectedPlanId);
                baseUnitAmount = decimal.Round(variantPrice * (100m - discountPercent) / 100m, 0, MidpointRounding.AwayFromZero);
            }
            decimal adjustedUnit = checked(baseUnitAmount + adjustments);
            return Ok(new
            {
                quote.ProductId, quote.SalesPlanId, quote.SalesPlanCode, quote.Quantity,
                listUnitAmount, discountPercent, unitAmount = adjustedUnit,
                totalAmount = checked(adjustedUnit * quote.Quantity), quote.Currency,
                quote.QuotedAtUtc,
                variantId = request.VariantId,
                planName = data.Plans.Single(plan => plan.Id == quote.SalesPlanId).Name,
                accessoryAmounts
            });
        }
        catch (SalesPlanUnavailableException exception)
        {
            return Conflict(new { code = exception.ReasonCode });
        }
        catch (InvalidDataException)
        {
            return BadRequest(new { code = "invalid-product-options" });
        }
    }

    [HttpPost("validate-cart")]
    public async Task<IActionResult> ValidateCart([FromBody] CartValidationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Lines is null || request.Lines.Count is < 1 or > 99)
            return BadRequest(new { code = "invalid-cart" });
        if (request.Lines.Any(line =>
                line is null || line.ProductId < 1 || string.IsNullOrWhiteSpace(line.SalesPlanId)) ||
            request.Lines.Select(line => $"{line.ProductId}:{line.VariantId?.ToString() ?? "base"}:{line.SalesPlanId}").Distinct(StringComparer.Ordinal)
                .Count() != request.Lines.Count)
            return BadRequest(new { code = "invalid-cart" });
        var result = new List<object>();
        foreach (CartLineRequest line in request.Lines)
        {
            IActionResult quoteResult = await Quote(new QuoteRequest(line.ProductId, line.SalesPlanId,
                line.Quantity, line.VariantId, line.PriceOptions, line.AccessoryIds), cancellationToken);
            if (quoteResult is not OkObjectResult { Value: { } value }) return quoteResult;
            result.Add(new { line.ProductId, line.SalesPlanId, line.Quantity, quote = value });
        }

        return Ok(new { valid = true, lines = result, checkedAtUtc = DateTimeOffset.UtcNow });
    }

    private async Task<(decimal, IReadOnlyDictionary<string, decimal>, int)> GetValidatedAdjustments(
        QuoteRequest request, ProductSalesOffer offer, CancellationToken cancellationToken)
    {
        ProductPurchaseConfiguration? product = await purchaseConfigurations.ReadAsync(request.ProductId, cancellationToken);
        if (product is null) throw new InvalidDataException();
        decimal adjustments = 0m;
        Dictionary<string, decimal> accessoryAmounts = new(StringComparer.Ordinal);
        HashSet<string> acceptedVariantKeys = new(StringComparer.Ordinal);
        foreach (ProductPurchaseOption feature in product.Options)
        {
            acceptedVariantKeys.Add(feature.Key);
            if (request.PriceOptions is null || !request.PriceOptions.TryGetValue(feature.Key, out string? value))
                throw new InvalidDataException();
            ProductPurchaseOptionValue? option = feature.Values.FirstOrDefault(item => item.Key == value);
            if (option is null) throw new InvalidDataException();
            adjustments += option.PriceDelta;
        }

        if (request.PriceOptions is not null && request.PriceOptions.Keys.Any(key => !acceptedVariantKeys.Contains(key)))
            throw new InvalidDataException();
        int maxQuantity = product.MaxPurchaseQuantity;
        maxQuantity = Math.Clamp(maxQuantity, 1, 99);
        if (request.AccessoryIds is { } accessoryIds &&
            accessoryIds.Count != accessoryIds.Distinct(StringComparer.Ordinal).Count())
            throw new InvalidDataException();
        HashSet<string> selected = new(request.AccessoryIds ?? [], StringComparer.Ordinal);
        HashSet<string> allowedAccessoryIds = product.Accessories.Select(accessory => accessory.Key).ToHashSet(StringComparer.Ordinal);
        if (selected.Any(id => !allowedAccessoryIds.Contains(id)) ||
            offer.AccessoryIds is { } allowedByOffer && selected.Any(id => !allowedByOffer.Contains(id, StringComparer.Ordinal)))
            throw new InvalidDataException();

        foreach (ProductAccessory accessory in product.Accessories)
        {
            if (!accessory.IsRequired && !selected.Contains(accessory.Key)) continue;
            if (!offer.AccessoryPriceAmounts.TryGetValue(accessory.Key, out decimal amount))
                throw new InvalidDataException();
            adjustments += amount;
            accessoryAmounts[accessory.Key] = amount;
        }

        return (adjustments, accessoryAmounts, maxQuantity);
    }

    public sealed record QuoteRequest(
        int ProductId,
        string? SalesPlanId,
        int Quantity,
        long? VariantId,
        Dictionary<string, string>? PriceOptions,
        IReadOnlyList<string>? AccessoryIds);

    public sealed record CartValidationRequest(IReadOnlyList<CartLineRequest>? Lines);

    public sealed record CartLineRequest(
        int ProductId,
        string SalesPlanId,
        int Quantity,
        long? VariantId,
        Dictionary<string, string>? PriceOptions,
        IReadOnlyList<string>? AccessoryIds);
}
