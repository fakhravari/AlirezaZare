using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shop.Controllers;
using Shop.Models;
using Shop.Services;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Domain.Warehouse.Entities;
using Shop.Database.Infrastructure.Persistence;

var confirmedPayment = new PaymentGatewayVerification(true, 250000m, "gateway-reference", "receipt-reference", DateTimeOffset.UtcNow);
if (!PaymentVerificationRules.Matches(250000m, "gateway-reference", confirmedPayment) ||
    PaymentVerificationRules.Matches(250001m, "gateway-reference", confirmedPayment) ||
    PaymentVerificationRules.Matches(250000m, "other-reference", confirmedPayment) ||
    PaymentVerificationRules.Matches(250000m, "gateway-reference", confirmedPayment with { Verified = false }) ||
    PaymentVerificationRules.Matches(250000m, "gateway-reference", confirmedPayment with { ReceiptReference = "" }) ||
    PaymentVerificationRules.Matches(250000m, "gateway-reference", confirmedPayment with { DefinitivelyFailed = true }) ||
    PaymentVerificationRules.Matches(250000m, "gateway-reference", confirmedPayment with { PaidAtUtc = null }) ||
    PaymentVerificationRules.Matches(250000m, "gateway-reference", confirmedPayment with { PaidAtUtc = DateTimeOffset.UtcNow.AddDays(1) }))
    throw new Exception("Payment verification accepted an unverified or mismatched transaction.");
var declinedPayment = confirmedPayment with { Verified = false, DefinitivelyFailed = true };
if (!PaymentVerificationRules.IsDefinitiveFailure("gateway-reference", declinedPayment) ||
    PaymentVerificationRules.IsDefinitiveFailure("another-reference", declinedPayment) ||
    PaymentVerificationRules.IsDefinitiveFailure("gateway-reference", declinedPayment with { DefinitivelyFailed = false }))
    throw new Exception("A payment failure was not tied to an authoritative matching reference.");
var gatewayStub = new UnconfiguredPaymentGateway();
if ((await gatewayStub.RequestAsync(Guid.NewGuid(), 250000m, CancellationToken.None)).Available ||
    (await gatewayStub.VerifyAsync("reference", 250000m, new QueryCollection(), CancellationToken.None)).Verified)
    throw new Exception("An unconfigured gateway must never report a payment as available or verified.");
Console.WriteLine("Payment contracts OK: unconfigured gateway fails closed and verified amount/reference are checked.");

string sourcePath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../fixtures/catalog.json"));
string source = File.ReadAllText(sourcePath);
CatalogSnapshot snapshot = CatalogSnapshot.Parse(source);
if (snapshot.Products.Count != 30 || snapshot.Brands.Count != 21)
    throw new Exception("Catalog extraction changed product/brand identity.");
if (snapshot.Products.Single(item => item.Id == 1).Slug != "cordless-hammer-drill")
    throw new Exception("Canonical product URL changed.");

int rejected = 0;

void Reject(Action<JsonNode> mutate)
{
    JsonNode node = JsonNode.Parse(source)!;
    mutate(node);
    try
    {
        CatalogSnapshot.Parse(node.ToJsonString());
    }
    catch (InvalidDataException)
    {
        rejected++;
        return;
    }

    throw new Exception("Invalid catalog was accepted.");
}

Reject(n => n["schemaVersion"] = 99);
Reject(n => n["products"]![0]!["id"] = 2);
Reject(n => n["products"]![0]!["code"] = "HN-002");
Reject(n => n["products"]![0]!["brandId"] = "missing");
Reject(n => n["products"]![0]!["categoryId"] = "missing");
Reject(n => n["products"]![0]!["categoryId"] = "drill-drivers");
Reject(n => n["categories"]!["cordless-drill"]!["parentId"] = "tools-and-equipment");
Reject(n => n["categories"]!["hammer-drills"]!["en"] = "");
Reject(n => n["categories"]!["hammer-drills"]!["slug"] = "Invalid Slug");
Reject(n => n["categories"]!["drill-drivers"]!["slug"] = "hammer-drills");
Reject(n => n["categories"]!.AsObject().Remove("power-tools"));
Reject(n => n["products"]![0]!["detailId"] = "missing");
Reject(n => n["products"]![0]!["mediaId"] = "missing");
Reject(n => n["products"]![0]!["colorIds"]![0] = "missing");
Reject(n => n["products"]![0]!["slug"] = "Invalid Slug");
Reject(n => n["products"]![0]!["price"] = -1);
Reject(n => n["brands"]!["bosch"]!["countryCode"] = "MISSING");
Reject(n => n["media"]!["product-1"]!["main"] = "content/products/images/main/../file.png");
Reject(n => n["media"]!["product-1"]!["card"] = "Content/Bad.jpg");
Reject(n => n["collections"]!["selected"]![0] = 999);
Reject(n => n["products"]![0]!["maxCartQuantity"] = 0);
Reject(n => n["products"]![0]!["discountPercent"] = 100);
Reject(n => n["specificationCategories"]!["lighting"]!["groupId"] = "missing");
Reject(n => n["specificationFeatures"]!["light-source"]!["categoryId"] = "missing");
Reject(n => n["products"]![15]!["specificationValues"]![0]!["featureId"] = "missing");
Reject(n => n["products"]![0]!["accessories"]![1]!["id"] = "shipping-insurance");
Reject(n => n["products"]![0]!["accessories"]![0]!["price"] = -1);

JsonNode injection = JsonNode.Parse(source)!;
injection["products"]![0]!["name"] = "</script><script>alert(1)</script>";
CatalogSnapshot safe = CatalogSnapshot.Parse(injection.ToJsonString());
if (safe.BrowserJson.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
    safe.BrowserJson.Contains("</script", StringComparison.OrdinalIgnoreCase))
    throw new Exception("Catalog JSON can escape its script element.");
Console.WriteLine(
    $"C# catalog contracts OK: {rejected} invalid catalogs rejected; canonical identity and safe JSON verified.");

string accountPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../fixtures/account-demo.json"));
string accountSource = File.ReadAllText(accountPath);
var account = Shop.Models.AccountData.Parse(accountSource);
if (account.Orders.Count != 10 || account.Coupons.Count != 10)
    throw new Exception("Account fixtures lost records.");

void RejectAccount(Action<JsonNode> mutate)
{
    var node = JsonNode.Parse(accountSource)!;
    mutate(node);
    try
    {
        Shop.Models.AccountData.Parse(node.ToJsonString());
    }
    catch (InvalidDataException)
    {
        return;
    }

    throw new Exception("Invalid account relationship was accepted.");
}

RejectAccount(n => n["schemaVersion"] = 99);
RejectAccount(n => n["currency"] = "IRR");
RejectAccount(n => n["orders"]![1]!["id"] = account.Orders[0].Id);
RejectAccount(n => n["orders"]![1]!["id"] = account.Orders[0].Id.ToUpperInvariant());
RejectAccount(n => n["orders"]![1]!["number"] = account.Orders[0].Number);
RejectAccount(n => n["orders"]![0]!["totalAmount"] = -1);
RejectAccount(n => n["orders"]![0]!["totalAmount"] = 1.5);
RejectAccount(n => n["orders"]![0]!["status"] = "unknown");
string[] requiredOrderStatuses =
[
    Shop.Models.AccountOrderStatus.PendingPayment,
    Shop.Models.AccountOrderStatus.PaymentConfirmed,
    Shop.Models.AccountOrderStatus.WarehouseConfirmed,
    Shop.Models.AccountOrderStatus.InTransit,
    Shop.Models.AccountOrderStatus.ShippedToPost,
    Shop.Models.AccountOrderStatus.Delivered,
    Shop.Models.AccountOrderStatus.Cancelled,
];
if (requiredOrderStatuses.Any(status => account.Orders.All(order => order.Status != status)))
    throw new Exception("Account fixtures must cover every order status.");
RejectAccount(n => n["coupons"]![0]!["orderIds"]![0] = "missing");
RejectAccount(n => n["coupons"]![0]!["discountPercent"] = 100);
RejectAccount(n => n["coupons"]![1]!["code"] = "BAZAR10");
RejectAccount(n => n["coupons"]![1]!["id"] = account.Coupons[0].Id.ToUpperInvariant());
var coupon = account.Coupons.First(c => c.Enabled);
if (!coupon.IsActive(coupon.ExpiresOn) || coupon.IsActive(coupon.ExpiresOn.AddDays(1)) ||
    (coupon with { Enabled = false }).IsActive(coupon.ExpiresOn))
    throw new Exception("Coupon expiry boundary is inconsistent.");
var encoded = System.Text.Json.JsonSerializer.Serialize(new { note = "</script><script>alert(1)</script>" },
    Shop.Models.AccountData.JsonOptions);
if (encoded.Contains("</script", StringComparison.OrdinalIgnoreCase))
    throw new Exception("Account JSON escaped its script element.");
Console.WriteLine("C# account contracts OK: relationships, amounts, expiry and safe JSON verified.");

var emailCodeStore = new EmailVerificationCodeStore();
const string testEmail = "otp-contract@example.test";
if (!emailCodeStore.TryCreate(testEmail, out string issuedCode, out _) || issuedCode.Length != 6)
    throw new Exception("Email OTP generation did not produce a six-digit code.");
if (emailCodeStore.TryCreate(testEmail, out _, out var retryAfter) || retryAfter <= TimeSpan.Zero)
    throw new Exception("Email OTP resend cooldown was not enforced.");
string wrongCode = issuedCode == "000000" ? "000001" : "000000";
if (emailCodeStore.Verify(testEmail, wrongCode) || !emailCodeStore.Verify(testEmail, issuedCode) ||
    emailCodeStore.Verify(testEmail, issuedCode))
    throw new Exception("Email OTP validation, consumption, or one-time use failed.");
Console.WriteLine("C# email OTP contracts OK: six digits, resend cooldown and one-time verification verified.");

const string salesPlanJson = """
                             {
                               "schemaVersion": 1,
                               "currency": "IRT",
                               "plans": [
                                 { "id": "regular", "code": "REGULAR", "name": "فروش عادی", "type": "regular", "isDefault": true, "isEnabled": true, "startsAtUtc": null, "endsAtUtc": null, "label": null, "showCountdown": false },
                                 { "id": "demo-time", "code": "DEMO-TIME", "name": "دموی زمانی", "type": "demo-time", "isDefault": false, "isEnabled": true, "startsAtUtc": "2026-10-30T00:00:00+00:00", "endsAtUtc": "2026-11-01T00:00:00+00:00", "label": "فروش ویژه", "showCountdown": true }
                               ],
                               "offers": [
                                 { "productId": 1, "salesPlanId": "regular", "priceAmount": 100000, "discountPercent": 0, "stockQuantity": 8, "maxPurchaseQuantity": 5, "isEnabled": true },
                                 { "productId": 1, "salesPlanId": "demo-time", "priceAmount": 80000, "discountPercent": 12.5, "stockQuantity": 2, "maxPurchaseQuantity": 2, "isEnabled": true }
                               ]
                             }
                             """;
var salesPlans = Shop.Models.SalesPlanCatalog.Parse(salesPlanJson, new HashSet<int> { 1 });
var salesPlanConfig = JsonNode.Parse(salesPlanJson)!;
var configuredOffers = (JsonArray)salesPlanConfig["offers"]!;
configuredOffers.RemoveAt(0);
configuredOffers[0]!["accessoryPriceAmounts"] = new JsonObject { ["belt"] = 245000 };
configuredOffers[0]!["accessoryIds"] = new JsonArray("belt");
configuredOffers[0]!["specificationOverrides"] = new JsonObject { ["نوع نمایش"] = "نمایش ویژهٔ همین طرح" };
const string accessoryCatalogJson = """
                                    {"products":[{"id":1,"price":1,"discountPercent":0,"stockQuantity":4,"maxCartQuantity":10,"accessories":[{"id":"belt","price":0.3,"required":true}]}]}
                                    """;
var configuredSalesPlans =
    Shop.Models.SalesPlanCatalog.ParseConfiguration(salesPlanConfig.ToJsonString(), accessoryCatalogJson);
if (configuredSalesPlans.Offers.Single(offer => offer.SalesPlanId == "regular").AccessoryPriceAmounts["belt"] !=
    300000 ||
    configuredSalesPlans.Offers.Single(offer => offer.SalesPlanId == "demo-time").AccessoryPriceAmounts["belt"] !=
    245000 ||
    !configuredSalesPlans.Offers.Single(offer => offer.SalesPlanId == "demo-time").AccessoryIds!.Contains("belt") ||
    configuredSalesPlans.Offers.Single(offer => offer.SalesPlanId == "demo-time").SpecificationOverrides["نوع نمایش"] !=
    "نمایش ویژهٔ همین طرح")
    throw new Exception("Per-plan accessory selection, specification override, or accessory price is inconsistent.");
var unknownAccessoryConfig = JsonNode.Parse(salesPlanConfig.ToJsonString())!;
unknownAccessoryConfig["offers"]![0]!["accessoryPriceAmounts"] = new JsonObject { ["unknown-part"] = 245000 };
try
{
    Shop.Models.SalesPlanCatalog.ParseConfiguration(unknownAccessoryConfig.ToJsonString(), accessoryCatalogJson);
    throw new Exception("A plan-specific price for an unknown accessory was accepted.");
}
catch (InvalidDataException)
{
}

var unknownAccessorySelectionConfig = JsonNode.Parse(salesPlanConfig.ToJsonString())!;
unknownAccessorySelectionConfig["offers"]![0]!["accessoryIds"] = new JsonArray("belt", "unknown-part");
try
{
    Shop.Models.SalesPlanCatalog.ParseConfiguration(unknownAccessorySelectionConfig.ToJsonString(),
        accessoryCatalogJson);
    throw new Exception("A plan-specific selection of an unknown accessory was accepted.");
}
catch (InvalidDataException)
{
}

var fallbackPriceConfig = JsonNode.Parse(salesPlanConfig.ToJsonString())!;
fallbackPriceConfig["offers"]![0]!.AsObject().Remove("accessoryPriceAmounts");
var fallbackPricePlans =
    Shop.Models.SalesPlanCatalog.ParseConfiguration(fallbackPriceConfig.ToJsonString(), accessoryCatalogJson);
if (fallbackPricePlans.Offers.Single(offer => offer.SalesPlanId == "demo-time").AccessoryPriceAmounts["belt"] != 300000)
    throw new Exception(
        "A plan without an accessory override must resolve the catalog accessory price before quoting.");
var unknownProductConfig = JsonNode.Parse(salesPlanConfig.ToJsonString())!;
unknownProductConfig["offers"]![0]!["productId"] = 404;
try
{
    Shop.Models.SalesPlanCatalog.ParseConfiguration(unknownProductConfig.ToJsonString(), accessoryCatalogJson);
    throw new Exception("A sales-plan offer for an unknown product was accepted.");
}
catch (InvalidDataException)
{
}

var missingOffersConfig = JsonNode.Parse(salesPlanConfig.ToJsonString())!;
missingOffersConfig["offers"] = null;
try
{
    Shop.Models.SalesPlanCatalog.ParseConfiguration(missingOffersConfig.ToJsonString(), accessoryCatalogJson);
    throw new Exception("A sales-plan configuration without offers was accepted.");
}
catch (InvalidDataException)
{
}

var ordinaryQuote = salesPlans.Quote(1, null, 1, DateTimeOffset.Parse("2026-10-31T00:00:00+00:00"));
var campaignQuote = salesPlans.Quote(1, "demo-time", 2, DateTimeOffset.Parse("2026-10-31T00:00:00+00:00"));
var campaignCartLine = Shop.Models.SalesPlanCartLine.FromQuote(campaignQuote, "variant-black");
var orderSnapshot =
    Shop.Models.SalesPlanOrderLineSnapshot.Capture(campaignQuote, "HN-001", "محصول نمونه", "variant-black");
if (ordinaryQuote.SalesPlanId != "regular" || ordinaryQuote.UnitAmount != 100000 ||
    campaignQuote.UnitAmount != 70000 || campaignQuote.TotalAmount != 140000 ||
    ordinaryQuote.Currency != "IRT" || campaignCartLine.SalesPlanId != "demo-time" ||
    campaignCartLine.LineKey != "1:variant-black:demo-time" || orderSnapshot.SalesPlanId != "demo-time" ||
    orderSnapshot.UnitAmount != 70000 || orderSnapshot.TotalAmount != 140000)
    throw new Exception("Default-plan or plan-specific quote is inconsistent.");
if (Shop.Models.SalesPlanLineIdentity.Create(1, "regular") ==
    Shop.Models.SalesPlanLineIdentity.Create(1, "demo-time"))
    throw new Exception("Different sales plans must create different cart/order line identities.");

void RejectSalesPlan(Action<JsonNode> mutate)
{
    var node = JsonNode.Parse(salesPlanJson)!;
    mutate(node);
    try
    {
        Shop.Models.SalesPlanCatalog.Parse(node.ToJsonString(), new HashSet<int> { 1 });
    }
    catch (InvalidDataException)
    {
        return;
    }

    throw new Exception("Invalid sales-plan catalog was accepted.");
}

RejectSalesPlan(n => n["plans"]![1]!["id"] = "regular");
RejectSalesPlan(n => n["offers"]![1]!["productId"] = 404);
RejectSalesPlan(n => n["offers"]![1]!["salesPlanId"] = "missing");
RejectSalesPlan(n => n["offers"]![1]!["stockQuantity"] = -1);
RejectSalesPlan(n => n["plans"]![1]!["endsAtUtc"] = "2026-10-30T00:00:00+00:00");
RejectSalesPlan(n => n["plans"]![1]!["isDefault"] = true);
RejectSalesPlan(n => n["plans"]![0]!["showCountdown"] = true);
RejectSalesPlan(n => n["offers"]![1]!["salesPlanId"] = "regular");
RejectSalesPlan(n => n["plans"]![1]!["startsAtUtc"] = "2026-10-30T03:30:00+03:30");
try
{
    salesPlans.Quote(1, "demo-time", 1, DateTimeOffset.Parse("2026-11-01T00:00:00+00:00"));
    throw new Exception("Expired sales plan was accepted.");
}
catch (Shop.Models.SalesPlanUnavailableException error) when (error.ReasonCode == "inactive-plan")
{
}

try
{
    salesPlans.Quote(1, "demo-time", 1, DateTimeOffset.Parse("2026-10-29T00:00:00+00:00"));
    throw new Exception("A not-yet-started sales plan was accepted.");
}
catch (Shop.Models.SalesPlanUnavailableException error) when (error.ReasonCode == "inactive-plan")
{
}

try
{
    salesPlans.Quote(1, "regular", 6, DateTimeOffset.Parse("2026-10-31T00:00:00+00:00"));
    throw new Exception("A quantity above the per-plan limit was accepted.");
}
catch (Shop.Models.SalesPlanUnavailableException error) when (error.ReasonCode == "purchase-limit")
{
}

Console.WriteLine(
    "C# sales-plan contracts OK: default selection, campaign pricing/stock/window, distinct line identity and invalid relations verified.");

SalesPlanCatalog expiredPlanCatalog = salesPlans with
{
    Plans = salesPlans.Plans.Select(plan => plan.Id == "demo-time"
        ? plan with { EndsAtUtc = DateTimeOffset.UnixEpoch }
        : plan).ToArray()
};
ProductController productController = new(new FixedCatalogSource(snapshot),
    new FixedSalesPlanSource(expiredPlanCatalog), Options.Create(new SeoMetadataOptions()))
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
};
RedirectResult expiredRedirect = (RedirectResult)(await productController.Details(
    "cordless-hammer-drill-1", "demo-time"));
if (!expiredRedirect.Permanent || expiredRedirect.Url != "/products/cordless-hammer-drill-1")
    throw new Exception("Expired plan URL did not permanently redirect to the canonical product URL.");
Console.WriteLine(
    "C# product route contract OK: expired sales-plan URLs permanently redirect to the base canonical URL.");

var modelOptions = new DbContextOptionsBuilder<ShopDbContext>()
    .UseSqlServer("Server=localhost;Database=unused;Integrated Security=True;TrustServerCertificate=True")
    .Options;
using (var modelContext = new ShopDbContext(modelOptions))
{
    var number = modelContext.Model.FindEntityType(typeof(WarehouseDocumentRecord))!.FindProperty(nameof(WarehouseDocumentRecord.DocumentNumber))!;
    if (number.GetDefaultValueSql() != "NEXT VALUE FOR [Warehouse].[DocumentNumberSequence]" ||
        number.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd)
        throw new Exception("New warehouse documents must receive their number from the database sequence.");
}
Console.WriteLine("C# warehouse document numbering contract OK.");

var countedCatalog = new CountingRuntimeDataset(source);
var requestCatalog = new CatalogRepository(countedCatalog);
await requestCatalog.ReadAsync();
await requestCatalog.ReadAsync();
if (countedCatalog.CatalogReads != 1 || countedCatalog.EditorialReads != 1)
    throw new Exception("Catalog was read more than once in one request scope.");
await new CatalogRepository(countedCatalog).ReadAsync();
if (countedCatalog.CatalogReads != 2 || countedCatalog.EditorialReads != 2)
    throw new Exception("A new request scope did not refresh the catalog.");
var countedPlans = new CountingSalesPlans();
var requestPlans = new DatabaseSalesPlanSource(countedPlans);
await requestPlans.ReadAsync();
await requestPlans.ReadAsync();
await new DatabaseSalesPlanSource(countedPlans).ReadAsync();
if (countedPlans.Reads != 2) throw new Exception("Sales plans were not reused within a request and refreshed in a new request.");
Console.WriteLine("Request-scoped catalog and sales-plan reuse OK.");

sealed class FixedCatalogSource(CatalogSnapshot snapshot) : ICatalogSource
{
    public ValueTask<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(snapshot);
}

sealed class FixedSalesPlanSource(SalesPlanCatalog snapshot) : ISalesPlanSource
{
    public ValueTask<SalesPlanCatalog> ReadAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(snapshot);
}


sealed class CountingRuntimeDataset(string source) : Shop.Database.Application.Store.Contracts.IStoreRuntimeDatasetRepository
{
    public int CatalogReads { get; private set; }
    public int EditorialReads { get; private set; }
    public Task<string?> ReadCatalogJsonAsync(CancellationToken cancellationToken) { CatalogReads++; return Task.FromResult<string?>(source); }
    public Task<string?> ReadBrandEditorialJsonAsync(CancellationToken cancellationToken) { EditorialReads++; return Task.FromResult<string?>("[]"); }
    public Task<string?> ReadAccountJsonAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<string?> ReadMagazineJsonAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<string> ReadGeographyJsonAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IReadOnlyList<Shop.Database.Application.Store.Contracts.StoreFaqRow>> ReadFaqAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}

sealed class CountingSalesPlans : Shop.Database.Application.Store.Contracts.IStoreSalesPlanRepository
{
    public int Reads { get; private set; }
    public Task<Shop.Database.Application.Store.Models.StoreSalesPlanSnapshot> ReadSalesPlanSnapshotAsync(CancellationToken cancellationToken)
    {
        Reads++;
        return Task.FromResult(new Shop.Database.Application.Store.Models.StoreSalesPlanSnapshot([], [], []));
    }
    public Task<Shop.Database.Application.Store.Models.ProductPurchaseConfiguration?> ReadProductPurchaseConfigurationAsync(int productId, CancellationToken cancellationToken) => throw new NotImplementedException();
}
