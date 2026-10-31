using System.Text.Json;
using System.Text.RegularExpressions;

namespace Shop.Models;

/// <summary>
/// Transport contract for a set of sales plans and product/plan offers.
/// Amounts are whole toman (IRT); timestamps must be UTC.
/// </summary>
public sealed record SalesPlanCatalog(
    int SchemaVersion,
    string Currency,
    IReadOnlyList<SalesPlan> Plans,
    IReadOnlyList<ProductSalesOffer> Offers)
{
    public const int CurrentSchemaVersion = 1;
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static SalesPlanCatalog Parse(string json, IReadOnlySet<int> productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);
        SalesPlanCatalog catalog = JsonSerializer.Deserialize<SalesPlanCatalog>(json, JsonOptions)
            ?? throw new InvalidDataException("Missing sales-plan catalog.");
        catalog.Validate(productIds);
        return catalog;
    }

    public static SalesPlanCatalog ParseConfiguration(string json, string catalogJson)
    {
        SalesPlanCatalog configured = JsonSerializer.Deserialize<SalesPlanCatalog>(json, JsonOptions)
            ?? throw new InvalidDataException("Missing sales-plan configuration.");
        if (configured.Plans is null || configured.Offers is null)
            throw new InvalidDataException("Sales-plan configuration must include plans and offers.");
        using JsonDocument catalogDocument = JsonDocument.Parse(catalogJson);
        JsonElement products = catalogDocument.RootElement.GetProperty("products");
        List<ProductSalesOffer> defaultOffers = [];
        HashSet<int> productIds = [];
        Dictionary<int, HashSet<string>> accessoryIds = [];
        Dictionary<int, Dictionary<string, decimal>> accessoryPricesByProduct = [];
        foreach (JsonElement product in products.EnumerateArray())
        {
            int id = product.GetProperty("id").GetInt32();
            decimal currentAmount = product.GetProperty("price").GetDecimal() * 1_000_000m;
            decimal discount = product.GetProperty("discountPercent").GetDecimal();
            decimal listAmount = decimal.Round(currentAmount * 100m / (100m - discount), 0, MidpointRounding.AwayFromZero);
            productIds.Add(id);
            HashSet<string> ids = [];
            Dictionary<string, decimal> accessoryPrices = new(StringComparer.Ordinal);
            JsonElement accessoryArray = product.TryGetProperty("accessories", out JsonElement accessoryElement) &&
                accessoryElement.ValueKind == JsonValueKind.Array ? accessoryElement : default;
            if (accessoryArray.ValueKind == JsonValueKind.Array)
            foreach (JsonElement accessory in accessoryArray.EnumerateArray())
            {
                string accessoryId = accessory.GetProperty("id").GetString()!;
                ids.Add(accessoryId);
                decimal accessoryAmount = accessory.GetProperty("price").GetDecimal() * 1_000_000m;
                accessoryPrices[accessoryId] = accessoryAmount;
            }
            accessoryIds[id] = ids;
            accessoryPricesByProduct[id] = accessoryPrices;
            defaultOffers.Add(new(id, "regular", listAmount, discount,
                product.GetProperty("stockQuantity").GetInt32(),
                product.GetProperty("maxCartQuantity").GetInt32(), true)
            {
                AccessoryPriceAmounts = accessoryPrices
            });
        }

        if (configured.Offers.Any(offer => offer is null || offer.SalesPlanId == "regular"))
            throw new InvalidDataException("The default-plan offer is derived from the product catalog and cannot be duplicated.");
        List<ProductSalesOffer> configuredOffers = [];
        foreach (ProductSalesOffer offer in configured.Offers)
        {
            if (!accessoryIds.TryGetValue(offer.ProductId, out HashSet<string>? productAccessoryIds) || productAccessoryIds is null)
                throw new InvalidDataException("An offer references an unknown product.");
            Dictionary<string, decimal> resolvedPrices = new(accessoryPricesByProduct[offer.ProductId], StringComparer.Ordinal);
            IReadOnlyDictionary<string, decimal> overrides = offer.AccessoryPriceAmounts ?? new Dictionary<string, decimal>();
            if (overrides.Keys.Any(key => !productAccessoryIds.Contains(key)))
                throw new InvalidDataException("An offer references an unknown accessory.");
            foreach ((string accessoryId, decimal amount) in overrides)
                resolvedPrices[accessoryId] = amount;
            configuredOffers.Add(offer with { AccessoryPriceAmounts = resolvedPrices });
        }
        SalesPlanCatalog complete = configured with
        {
            Offers = defaultOffers.Concat(configuredOffers).ToArray()
        };
        complete.Validate(productIds);
        return complete;
    }

    public void Validate(IReadOnlySet<int> productIds)
    {
        if (SchemaVersion != CurrentSchemaVersion || Currency != "IRT" ||
            Plans is null || Plans.Count == 0 || Offers is null)
            throw new InvalidDataException("Unsupported sales-plan schema, currency, or collections.");

        HashSet<string> planIds = new(StringComparer.Ordinal);
        HashSet<string> planCodes = new(StringComparer.OrdinalIgnoreCase);
        int defaults = 0;
        foreach (SalesPlan plan in Plans)
        {
            if (plan is null || !IsIdentifier(plan.Id) || !planIds.Add(plan.Id) ||
                !IsCode(plan.Code) || !planCodes.Add(plan.Code) ||
                string.IsNullOrWhiteSpace(plan.Name) || !IsIdentifier(plan.Type) ||
                (plan.StartsAtUtc is { } start && !IsUtc(start)) ||
                (plan.EndsAtUtc is { } end && !IsUtc(end)) ||
                (plan.StartsAtUtc is { } starts && plan.EndsAtUtc is { } ends && ends <= starts) ||
                (plan.Label is { Length: > 80 }) ||
                (plan.IsDefault && (plan.Type != "regular" || plan.Label is not null || plan.ShowCountdown)) ||
                (plan.ShowCountdown && plan.EndsAtUtc is null))
                throw new InvalidDataException("Invalid or duplicate sales plan.");

            if (plan.IsDefault) defaults++;
        }

        if (defaults != 1)
            throw new InvalidDataException("Exactly one regular default sales plan is required.");

        HashSet<string> offerKeys = new(StringComparer.Ordinal);
        foreach (ProductSalesOffer offer in Offers)
        {
            if (offer is null) throw new InvalidDataException("Sales-plan offers cannot be null.");
            string key = $"{offer.ProductId}:{offer.SalesPlanId}";
            if (offer.ProductId <= 0 || !productIds.Contains(offer.ProductId) ||
                !planIds.Contains(offer.SalesPlanId) || !offerKeys.Add(key) ||
                offer.PriceAmount < 0 || offer.PriceAmount != decimal.Truncate(offer.PriceAmount) ||
                offer.AccessoryPriceAmounts is null || offer.AccessoryPriceAmounts.Any(price =>
                    string.IsNullOrWhiteSpace(price.Key) || price.Value < 0 || price.Value != decimal.Truncate(price.Value)) ||
                offer.DiscountPercent is < 0 or >= 100 ||
                offer.StockQuantity < 0 || offer.MaxPurchaseQuantity is < 1 or > 99)
                throw new InvalidDataException("Invalid, duplicate, or unreferenced product sales offer.");
        }

        string defaultPlanId = Plans.Single(plan => plan.IsDefault).Id;
        foreach (int productId in productIds)
            if (!offerKeys.Contains($"{productId}:{defaultPlanId}"))
                throw new InvalidDataException("Every catalog product must have a default-plan offer.");
    }

    public SalesPlanQuote Quote(int productId, string? requestedPlanId, int quantity, DateTimeOffset nowUtc)
    {
        if (!IsUtc(nowUtc)) throw new ArgumentException("Quote time must be UTC.", nameof(nowUtc));
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));

        string planId = requestedPlanId ?? Plans.Single(plan => plan.IsDefault).Id;
        SalesPlan plan = Plans.SingleOrDefault(candidate => candidate.Id == planId)
            ?? throw new SalesPlanUnavailableException("unknown-plan");
        if (!plan.IsEnabled ||
            (plan.StartsAtUtc is { } starts && nowUtc < starts) ||
            (plan.EndsAtUtc is { } ends && nowUtc >= ends))
            throw new SalesPlanUnavailableException("inactive-plan");

        ProductSalesOffer offer = Offers.SingleOrDefault(candidate =>
            candidate.ProductId == productId && candidate.SalesPlanId == plan.Id)
            ?? throw new SalesPlanUnavailableException("product-not-in-plan");
        if (!offer.IsEnabled) throw new SalesPlanUnavailableException("offer-disabled");
        if (offer.StockQuantity < quantity) throw new SalesPlanUnavailableException("insufficient-stock");
        if (offer.MaxPurchaseQuantity < quantity) throw new SalesPlanUnavailableException("purchase-limit");

        decimal unitPrice = decimal.Round(
            offer.PriceAmount * (100m - offer.DiscountPercent) / 100m,
            0,
            MidpointRounding.AwayFromZero);
        return new(productId, plan.Id, plan.Code, quantity, offer.PriceAmount,
            offer.DiscountPercent, unitPrice, checked(unitPrice * quantity), Currency, nowUtc);
    }

    private static bool IsUtc(DateTimeOffset value) => value.Offset == TimeSpan.Zero;

    private static bool IsIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 64 &&
        Regex.IsMatch(value, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);

    private static bool IsCode(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 64 &&
        Regex.IsMatch(value, "^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$", RegexOptions.CultureInvariant);
}

public sealed record SalesPlan(
    string Id,
    string Code,
    string Name,
    string Type,
    bool IsDefault,
    bool IsEnabled,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string? Label,
    bool ShowCountdown);

public sealed record ProductSalesOffer(
    int ProductId,
    string SalesPlanId,
    decimal PriceAmount,
    decimal DiscountPercent,
    int StockQuantity,
    int MaxPurchaseQuantity,
    bool IsEnabled)
{
    public IReadOnlyDictionary<string, decimal> AccessoryPriceAmounts { get; init; } = new Dictionary<string, decimal>();
}

/// <summary>Immutable monetary facts captured for cart/order history.</summary>
public sealed record SalesPlanQuote(
    int ProductId,
    string SalesPlanId,
    string SalesPlanCode,
    int Quantity,
    decimal ListUnitAmount,
    decimal DiscountPercent,
    decimal UnitAmount,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset QuotedAtUtc);

/// <summary>Resolved cart line; plan identity is mandatory after quote validation.</summary>
public sealed record SalesPlanCartLine(
    int ProductId,
    string SalesPlanId,
    string? VariantId,
    int Quantity,
    SalesPlanQuote PriceSnapshot)
{
    public static SalesPlanCartLine FromQuote(SalesPlanQuote quote, string? variantId = null) =>
        new(quote.ProductId, quote.SalesPlanId, variantId, quote.Quantity, quote);

    public string LineKey => SalesPlanLineIdentity.Create(ProductId, SalesPlanId, VariantId);
}

/// <summary>
/// Immutable order-line facts. Keep this snapshot when a plan or product changes later.
/// </summary>
public sealed record SalesPlanOrderLineSnapshot(
    int ProductId,
    string ProductCode,
    string ProductName,
    string SalesPlanId,
    string SalesPlanCode,
    string? VariantId,
    int Quantity,
    decimal ListUnitAmount,
    decimal DiscountPercent,
    decimal UnitAmount,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CapturedAtUtc)
{
    public static SalesPlanOrderLineSnapshot Capture(
        SalesPlanQuote quote, string productCode, string productName, string? variantId = null)
    {
        if (string.IsNullOrWhiteSpace(productCode) || string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Order snapshots require product code and name.");
        return new(quote.ProductId, productCode.Trim(), productName.Trim(), quote.SalesPlanId,
            quote.SalesPlanCode, variantId, quote.Quantity, quote.ListUnitAmount,
            quote.DiscountPercent, quote.UnitAmount, quote.TotalAmount, quote.Currency,
            quote.QuotedAtUtc);
    }
}

public sealed class SalesPlanUnavailableException(string reasonCode)
    : InvalidOperationException(reasonCode)
{
    public string ReasonCode { get; } = reasonCode;
}

public static class SalesPlanLineIdentity
{
    /// <summary>Different plans remain distinct cart/order lines.</summary>
    public static string Create(int productId, string salesPlanId, string? variantId = null)
    {
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        if (string.IsNullOrWhiteSpace(salesPlanId)) throw new ArgumentException("A sales plan is required.", nameof(salesPlanId));
        return $"{productId}:{variantId?.Trim() ?? "base"}:{salesPlanId.Trim()}";
    }
}
