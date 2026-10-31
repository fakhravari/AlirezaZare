using System.Text.Json.Nodes;
using Shop.Services;

string sourcePath = args.Length > 0 ? Path.GetFullPath(args[0]) :
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Shop/wwwroot/content/data/catalog.json"));
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
    try { CatalogSnapshot.Parse(node.ToJsonString()); }
    catch (InvalidDataException) { rejected++; return; }
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
if (safe.BrowserJson.Contains("<script", StringComparison.OrdinalIgnoreCase) || safe.BrowserJson.Contains("</script", StringComparison.OrdinalIgnoreCase))
    throw new Exception("Catalog JSON can escape its script element.");
Console.WriteLine($"C# catalog contracts OK: {rejected} invalid catalogs rejected; canonical identity and safe JSON verified.");

string accountPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, "../../../App_Data/account-demo.json"));
string accountSource = File.ReadAllText(accountPath);
var account = Shop.Models.AccountData.Parse(accountSource);
if (account.Orders.Count != 10 || account.Coupons.Count != 10)
    throw new Exception("Account fixtures lost records.");
void RejectAccount(Action<JsonNode> mutate)
{
    var node = JsonNode.Parse(accountSource)!;
    mutate(node);
    try { Shop.Models.AccountData.Parse(node.ToJsonString()); }
    catch (InvalidDataException) { return; }
    throw new Exception("Invalid account relationship was accepted.");
}
RejectAccount(n => n["schemaVersion"] = 99);
RejectAccount(n => n["currency"] = "IRR");
RejectAccount(n => n["orders"]![1]!["id"] = "order-1");
RejectAccount(n => n["orders"]![0]!["totalAmount"] = -1);
RejectAccount(n => n["orders"]![0]!["totalAmount"] = 1.5);
RejectAccount(n => n["orders"]![0]!["status"] = "unknown");
RejectAccount(n => n["coupons"]![0]!["orderIds"]![0] = "missing");
RejectAccount(n => n["coupons"]![0]!["discountPercent"] = 100);
RejectAccount(n => n["coupons"]![1]!["code"] = "BAZAR10");
var coupon = account.Coupons.First(c => c.Enabled);
if (!coupon.IsActive(coupon.ExpiresOn) || coupon.IsActive(coupon.ExpiresOn.AddDays(1)) ||
    (coupon with { Enabled = false }).IsActive(coupon.ExpiresOn))
    throw new Exception("Coupon expiry boundary is inconsistent.");
var encoded = System.Text.Json.JsonSerializer.Serialize(new { note = "</script><script>alert(1)</script>" }, Shop.Models.AccountData.JsonOptions);
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
if (emailCodeStore.Verify(testEmail, wrongCode) || !emailCodeStore.Verify(testEmail, issuedCode) || emailCodeStore.Verify(testEmail, issuedCode))
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
const string accessoryCatalogJson = """
{"products":[{"id":1,"price":1,"discountPercent":0,"stockQuantity":4,"maxCartQuantity":10,"accessories":[{"id":"belt","price":0.3,"required":true}]}]}
""";
var configuredSalesPlans = Shop.Models.SalesPlanCatalog.ParseConfiguration(salesPlanConfig.ToJsonString(), accessoryCatalogJson);
if (configuredSalesPlans.Offers.Single(offer => offer.SalesPlanId == "regular").AccessoryPriceAmounts["belt"] != 300000 ||
    configuredSalesPlans.Offers.Single(offer => offer.SalesPlanId == "demo-time").AccessoryPriceAmounts["belt"] != 245000)
    throw new Exception("Per-plan accessory price override or default catalog conversion is inconsistent.");
var unknownAccessoryConfig = JsonNode.Parse(salesPlanConfig.ToJsonString())!;
unknownAccessoryConfig["offers"]![0]!["accessoryPriceAmounts"] = new JsonObject { ["unknown-part"] = 245000 };
try
{
    Shop.Models.SalesPlanCatalog.ParseConfiguration(unknownAccessoryConfig.ToJsonString(), accessoryCatalogJson);
    throw new Exception("A plan-specific price for an unknown accessory was accepted.");
}
catch (InvalidDataException) { }
var fallbackPriceConfig = JsonNode.Parse(salesPlanConfig.ToJsonString())!;
fallbackPriceConfig["offers"]![0]!.AsObject().Remove("accessoryPriceAmounts");
var fallbackPricePlans = Shop.Models.SalesPlanCatalog.ParseConfiguration(fallbackPriceConfig.ToJsonString(), accessoryCatalogJson);
if (fallbackPricePlans.Offers.Single(offer => offer.SalesPlanId == "demo-time").AccessoryPriceAmounts["belt"] != 300000)
    throw new Exception("A plan without an accessory override must resolve the catalog accessory price before quoting.");
var unknownProductConfig = JsonNode.Parse(salesPlanConfig.ToJsonString())!;
unknownProductConfig["offers"]![0]!["productId"] = 404;
try
{
    Shop.Models.SalesPlanCatalog.ParseConfiguration(unknownProductConfig.ToJsonString(), accessoryCatalogJson);
    throw new Exception("A sales-plan offer for an unknown product was accepted.");
}
catch (InvalidDataException) { }
var missingOffersConfig = JsonNode.Parse(salesPlanConfig.ToJsonString())!;
missingOffersConfig["offers"] = null;
try
{
    Shop.Models.SalesPlanCatalog.ParseConfiguration(missingOffersConfig.ToJsonString(), accessoryCatalogJson);
    throw new Exception("A sales-plan configuration without offers was accepted.");
}
catch (InvalidDataException) { }
var ordinaryQuote = salesPlans.Quote(1, null, 1, DateTimeOffset.Parse("2026-10-31T00:00:00+00:00"));
var campaignQuote = salesPlans.Quote(1, "demo-time", 2, DateTimeOffset.Parse("2026-10-31T00:00:00+00:00"));
var campaignCartLine = Shop.Models.SalesPlanCartLine.FromQuote(campaignQuote, "variant-black");
var orderSnapshot = Shop.Models.SalesPlanOrderLineSnapshot.Capture(campaignQuote, "HN-001", "محصول نمونه", "variant-black");
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
    try { Shop.Models.SalesPlanCatalog.Parse(node.ToJsonString(), new HashSet<int> { 1 }); }
    catch (InvalidDataException) { return; }
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
catch (Shop.Models.SalesPlanUnavailableException error) when (error.ReasonCode == "inactive-plan") { }
try
{
    salesPlans.Quote(1, "demo-time", 1, DateTimeOffset.Parse("2026-10-29T00:00:00+00:00"));
    throw new Exception("A not-yet-started sales plan was accepted.");
}
catch (Shop.Models.SalesPlanUnavailableException error) when (error.ReasonCode == "inactive-plan") { }
try
{
    salesPlans.Quote(1, "regular", 6, DateTimeOffset.Parse("2026-10-31T00:00:00+00:00"));
    throw new Exception("A quantity above the per-plan limit was accepted.");
}
catch (Shop.Models.SalesPlanUnavailableException error) when (error.ReasonCode == "purchase-limit") { }
Console.WriteLine("C# sales-plan contracts OK: default selection, campaign pricing/stock/window, distinct line identity and invalid relations verified.");
