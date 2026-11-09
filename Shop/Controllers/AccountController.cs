using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;
using Shop.Services;
using Shop.Models;
using Shop.Database.Domain.Account.Entities;

namespace Shop.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "ShopCookie")]
[Route("api/account")]
public sealed class AccountController(
    ShopDbContext db,
    ISalesPlanSource plans,
    IProductPurchaseConfigurationSource purchaseConfigurations) : ControllerBase
{
    private long ProfileId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("state")]
    public async Task<IActionResult> State(CancellationToken ct)
    {
        const string sql = """
                           SELECT (SELECT p.[Id] id,p.[Email] email,p.[FirstName] firstName,p.[LastName] lastName,p.[Phone] phone,p.[Gender] gender,p.[NewsletterEnabled] newsletter,
                             JSON_QUERY((SELECT ci.[IdProduct] id,ci.[IdProductVariant] variantId,ci.[Quantity] quantity,sp.[SalesPlanKey] salesPlanId,JSON_QUERY(ci.[OptionsJson]) options FROM [Account].[Cart] c JOIN [Account].[CartItem] ci ON ci.[IdCart]=c.[Id] JOIN [Store].[SalesPlan] sp ON sp.[Id]=ci.[IdSalesPlan] WHERE c.[IdProfile]={0} FOR JSON PATH)) cart,
                             JSON_QUERY((SELECT a.[Id] id,a.[Title] title,a.[Recipient] recipient,a.[Phone] phone,a.[Country] country,a.[CountryId] countryId,a.[Province] province,a.[ProvinceId] provinceId,a.[City] city,a.[CityId] cityId,a.[Street] street,a.[PostalCode] postal,a.[IsDefault] isDefault FROM [Account].[Address] a WHERE a.[IdProfile]={0} FOR JSON PATH)) addresses,
                             JSON_QUERY((SELECT f.[IdProduct] id FROM [Account].[Favorite] f WHERE f.[IdProfile]={0} FOR JSON PATH)) favorites,
                             JSON_QUERY((SELECT x.[IdProduct] id FROM [Account].[CompareItem] x WHERE x.[IdProfile]={0} FOR JSON PATH)) compare,
                             JSON_QUERY((SELECT pa.[IdProduct] productId,pa.[Scope] scope,JSON_QUERY(pa.[VariantsJson]) variants,pa.[PriceAtRegistration] priceAtRegistration,pa.[StockAtRegistration] stockAtRegistration,pa.[CreatedAtUtc] createdAt FROM [Account].[PriceAlert] pa WHERE pa.[IdProfile]={0} FOR JSON PATH)) priceAlerts,
                             JSON_QUERY((SELECT a.[ArticleKey] slug,ai.[InteractionType] type FROM [Account].[ArticleInteraction] ai JOIN [Mag].[MagazineArticle] a ON a.[Id]=ai.[IdMagazineArticle] WHERE ai.[IdProfile]={0} FOR JSON PATH)) articleInteractions,
                             JSON_QUERY((SELECT a.[ArticleKey] slug,af.[DisplayName] name,af.[FeedbackText] text,af.[Rating] rating,af.[Status] status,af.[CreatedAtUtc] createdAt FROM [Account].[ArticleFeedback] af JOIN [Mag].[MagazineArticle] a ON a.[Id]=af.[IdMagazineArticle] WHERE af.[IdProfile]={0} FOR JSON PATH)) articleFeedback,
                             JSON_QUERY((SELECT cp.[IdSelectedAddress] selectedAddressId,cp.[PaymentMethod] paymentMethod FROM [Account].[CheckoutPreference] cp WHERE cp.[IdProfile]={0} FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)) checkout,
                             JSON_QUERY((SELECT o.[Id] id,o.[OrderNumber] number,o.[Status] status,o.[OrderedAtUtc] orderedAtUtc,o.[ItemCount] itemCount,o.[TotalAmount] totalAmount,JSON_QUERY((SELECT i.[IdProduct] productId,i.[IdProductVariant] variantId,i.[ProductNameSnapshot] name,sp.[SalesPlanKey] salesPlanId,i.[Quantity] quantity,i.[UnitAmount] unitAmount,JSON_QUERY(i.[OptionsJson]) options FROM [Account].[CustomerOrderItem] i JOIN [Store].[SalesPlan] sp ON sp.[Id]=i.[IdSalesPlan] WHERE i.[IdCustomerOrder]=o.[Id] FOR JSON PATH)) items FROM [Account].[CustomerOrder] o WHERE o.[IdProfile]={0} ORDER BY o.[Id] DESC FOR JSON PATH)) orders
                           FROM [Account].[Profile] p WHERE p.[Id]={0} FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) AS [Value];
                           """;
        var json = await db.Database.SqlQueryRaw<string>(sql, ProfileId).SingleOrDefaultAsync(ct);
        return json is null ? NotFound() : Content(json, "application/json; charset=utf-8");
    }

    [HttpGet("recent-searches")]
    public async Task<IActionResult> RecentSearches(CancellationToken ct)
    {
        var items = await db.AccountRecentSearches.AsNoTracking()
            .Where(x => x.IdProfile == ProfileId)
            .OrderByDescending(x => x.SearchedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => x.SearchText).Take(6).ToListAsync(ct);
        return Ok(new { items });
    }

    [HttpPost("recent-searches")]
    public async Task<IActionResult> AddRecentSearch([FromBody] RecentSearchRequest request, CancellationToken ct)
    {
        string searchText = (request.Query ?? string.Empty).Trim();
        if (searchText.Length is < 3 or > 80) return BadRequest(new { code = "invalid-search-query" });
        string searchKey = searchText.ToLowerInvariant();
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var existing = await db.AccountRecentSearches
            .SingleOrDefaultAsync(x => x.IdProfile == ProfileId && x.SearchKey == searchKey, ct);
        if (existing is null)
        {
            db.AccountRecentSearches.Add(new AccountRecentSearch
            {
                IdProfile = ProfileId,
                SearchKey = searchKey,
                SearchText = searchText,
                SearchedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.SearchText = searchText;
            existing.SearchedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        var keep = await db.AccountRecentSearches.Where(x => x.IdProfile == ProfileId)
            .OrderByDescending(x => x.SearchedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => x.Id).Take(6).ToListAsync(ct);
        await db.AccountRecentSearches.Where(x => x.IdProfile == ProfileId && !keep.Contains(x.Id))
            .ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { saved = true });
    }

    [HttpDelete("recent-searches")]
    public async Task<IActionResult> ClearRecentSearches(CancellationToken ct)
    {
        await db.AccountRecentSearches.Where(x => x.IdProfile == ProfileId).ExecuteDeleteAsync(ct);
        return Ok(new { deleted = true });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(ProfileRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
            request.FirstName.Length > 100 || request.LastName.Length > 100 || (request.Phone?.Length ?? 0) > 30 ||
            request.Gender is not (null or "male" or "female")) return BadRequest(new { code = "invalid-profile" });
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Account].[Profile] SET [FirstName]={request.FirstName.Trim()},[LastName]={request.LastName.Trim()},[Phone]={request.Phone},[Gender]={request.Gender},[NewsletterEnabled]={request.Newsletter},[UpdatedAtUtc]=SYSUTCDATETIME() WHERE [Id]={ProfileId}",
            ct);
        return Ok(new { saved = true });
    }

    [HttpPut("cart")]
    public async Task<IActionResult> SaveCart([FromBody] CartRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count > 99 || request.Items.Any(i =>
                i.ProductId < 1 || i.VariantId is < 1 || i.Quantity is < 1 or > 99 ||
                string.IsNullOrWhiteSpace(i.SalesPlanId) || i.SalesPlanId.Length > 64))
            return BadRequest(new { code = "invalid-cart" });
        var duplicate = request.Items.GroupBy(i => (i.ProductId, i.SalesPlanId, i.VariantId)).Any(g => g.Count() > 1);
        if (duplicate) return BadRequest(new { code = "duplicate-cart-line" });
        SalesPlanCatalog currentCatalog = await plans.ReadAsync(ct);
        foreach (var item in request.Items)
        {
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
        }

        string payload = System.Text.Json.JsonSerializer.Serialize(request.Items,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        await db.Database.ExecuteSqlInterpolatedAsync($$"""
                                                        SET XACT_ABORT ON; BEGIN TRANSACTION;
                                                        IF NOT EXISTS(SELECT 1 FROM [Account].[Cart] WHERE [IdProfile]={{ProfileId}}) INSERT INTO [Account].[Cart]([IdProfile]) VALUES({{ProfileId}});
                                                        DECLARE @CartId BIGINT=(SELECT [Id] FROM [Account].[Cart] WHERE [IdProfile]={{ProfileId}});
                                                        DELETE FROM [Account].[CartItem] WHERE [IdCart]=@CartId;
                                                        INSERT INTO [Account].[CartItem]([IdCart],[IdProduct],[IdProductVariant],[IdSalesPlan],[Quantity],[OptionsJson])
                                                        SELECT @CartId,p.[Id],j.[VariantId],sp.[Id],j.[Quantity],COALESCE(j.[OptionsJson],N'{}')
                                                        FROM OPENJSON({{payload}}) WITH([ProductId] BIGINT '$.productId',[VariantId] BIGINT '$.variantId',[SalesPlanId] NVARCHAR(64) '$.salesPlanId',[Quantity] INT '$.quantity',[OptionsJson] NVARCHAR(MAX) '$.options' AS JSON) j
                                                        JOIN [Store].[Product] p ON p.[Id]=j.[ProductId] AND p.[IsActive]=1
                                                        JOIN [Store].[SalesPlan] sp ON sp.[SalesPlanKey]=j.[SalesPlanId] AND sp.[IsEnabled]=1;
                                                        IF @@ROWCOUNT<>(SELECT COUNT(*) FROM OPENJSON({{payload}})) THROW 51001,'Cart contains an unavailable product.',1;
                                                        UPDATE [Account].[Cart] SET [UpdatedAtUtc]=SYSUTCDATETIME() WHERE [Id]=@CartId;
                                                        COMMIT TRANSACTION;
                                                        """, ct);
        return Ok(new { saved = true });
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress(AddressRequest a, CancellationToken ct)
    {
        if (new[]
            {
                a.Title, a.Recipient, a.Phone, a.Country, a.CountryId, a.Province, a.ProvinceId, a.City, a.CityId,
                a.Street, a.Postal
            }.Any(v => string.IsNullOrWhiteSpace(v)) ||
            a.Street!.Length > 500) return BadRequest(new { code = "invalid-address" });
        List<long> inserted = await db.Database.SqlQuery<long>(
                $"INSERT INTO [Account].[Address]([IdProfile],[Title],[Recipient],[Phone],[Country],[CountryId],[Province],[ProvinceId],[City],[CityId],[Street],[PostalCode]) OUTPUT INSERTED.[Id] AS [Value] VALUES({ProfileId},{a.Title!.Trim()},{a.Recipient!.Trim()},{a.Phone!.Trim()},{a.Country!.Trim()},{a.CountryId!.Trim()},{a.Province!.Trim()},{a.ProvinceId!.Trim()},{a.City!.Trim()},{a.CityId!.Trim()},{a.Street!.Trim()},{a.Postal!.Trim()})")
            .ToListAsync(ct);
        long id = inserted.Single();
        return Ok(new { saved = true, id = id.ToString(System.Globalization.CultureInfo.InvariantCulture) });
    }

    [HttpPut("checkout-preference")]
    public async Task<IActionResult> SaveCheckoutPreference([FromBody] CheckoutPreferenceRequest request,
        CancellationToken ct)
    {
        if (request.AddressId is null or < 1 || request.PaymentMethod != "zarinpal")
            return BadRequest(new { code = "invalid-checkout-preference" });
        int owned = await db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [Account].[Address] WHERE [Id]={0} AND [IdProfile]={1}",
                request.AddressId, ProfileId).SingleAsync(ct);
        if (owned == 0) return BadRequest(new { code = "address-not-owned" });
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"IF NOT EXISTS(SELECT 1 FROM [Account].[CheckoutPreference] WHERE [IdProfile]={ProfileId}) INSERT INTO [Account].[CheckoutPreference]([IdProfile],[IdSelectedAddress],[PaymentMethod]) VALUES({ProfileId},{request.AddressId},{request.PaymentMethod}); ELSE UPDATE [Account].[CheckoutPreference] SET [IdSelectedAddress]={request.AddressId},[PaymentMethod]={request.PaymentMethod},[UpdatedAtUtc]=SYSUTCDATETIME() WHERE [IdProfile]={ProfileId};",
            ct);
        return Ok(new { saved = true });
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        if (request.Lines is null || request.Lines.Count is < 1 or > 99 || request.AddressId < 1 ||
            request.PaymentMethod != "zarinpal") return BadRequest(new { code = "invalid-checkout" });
        if (request.Lines.Any(i =>
                i.ProductId < 1 || i.VariantId is < 1 || i.Quantity is < 1 or > 99 ||
                string.IsNullOrWhiteSpace(i.SalesPlanId)) ||
            request.Lines.Select(i => (i.ProductId, i.SalesPlanId, i.VariantId)).Distinct().Count() !=
            request.Lines.Count) return BadRequest(new { code = "invalid-cart" });
        string? addressSnapshot = await db.Database.SqlQueryRaw<string>(
            "SELECT (SELECT [Title] title,[Recipient] recipient,[Phone] phone,[Country] country,[Province] province,[City] city,[Street] street,[PostalCode] postal FROM [Account].[Address] WHERE [Id]={0} AND [IdProfile]={1} FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) AS [Value]",
            request.AddressId, ProfileId).SingleOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(addressSnapshot)) return BadRequest(new { code = "address-not-owned" });
        var catalog = await plans.ReadAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var priced = new List<(CartLineRequest Line, decimal UnitAmount, string ProductName)>();
        try
        {
            foreach (var line in request.Lines)
            {
                if (line.VariantId.HasValue &&
                    !await ProductVariantExistsAsync(line.ProductId, line.VariantId.Value, ct))
                    return Conflict(new { code = "product-variant-unavailable" });
                var quote = catalog.Quote(checked((int)line.ProductId), line.SalesPlanId, line.Quantity, now);
                ProductInventoryVariant[] warehouseVariants = catalog.InventoryVariants
                    .Where(variant => variant.ProductId == line.ProductId).ToArray();
                if (warehouseVariants.Length > 0)
                {
                    ProductInventoryVariant? selectedWarehouseVariant = line.VariantId is { } inventoryVariantId
                        ? warehouseVariants.SingleOrDefault(variant => variant.VariantId == inventoryVariantId)
                        : null;
                    if (selectedWarehouseVariant is null ||
                        !selectedWarehouseVariant.StockBySalesPlan.TryGetValue(line.SalesPlanId,
                            out decimal variantStock) ||
                        variantStock < line.Quantity)
                        return Conflict(new { code = "inventory-variant-or-stock-changed" });
                }
                else if (line.VariantId.HasValue)
                    return Conflict(new { code = "product-variant-unavailable" });

                string? productName = await db.Database
                    .SqlQueryRaw<string>(
                        "SELECT [Name] AS [Value] FROM [Store].[Product] WHERE [Id]={0} AND [IsActive]=1",
                        line.ProductId).SingleOrDefaultAsync(ct);
                if (productName is null) return Conflict(new { code = "product-unavailable" });
                ProductPurchaseConfiguration? configuration =
                    await purchaseConfigurations.ReadAsync(checked((int)line.ProductId), ct);
                if (configuration is null) return Conflict(new { code = "product-options-unavailable" });
                if (line.Quantity > configuration.MaxPurchaseQuantity) return Conflict(new { code = "purchase-limit" });
                var variantValues = new Dictionary<string, string>(StringComparer.Ordinal);
                var accessoryIds = new HashSet<string>(StringComparer.Ordinal);
                if (line.Options is { ValueKind: System.Text.Json.JsonValueKind.Object } options)
                {
                    if (options.TryGetProperty("priceOptions", out var priceOptions) &&
                        priceOptions.ValueKind == System.Text.Json.JsonValueKind.Array ||
                        options.TryGetProperty("variants", out priceOptions) &&
                        priceOptions.ValueKind == System.Text.Json.JsonValueKind.Array)
                        foreach (var option in priceOptions.EnumerateArray())
                            if (option.TryGetProperty("key", out var key) &&
                                option.TryGetProperty("productValue", out var value) && key.GetString() is { } k &&
                                value.GetString() is { } v)
                                variantValues[k] = v;
                    if (options.TryGetProperty("accessories", out var accessories) &&
                        accessories.ValueKind == System.Text.Json.JsonValueKind.Array)
                        foreach (var accessory in accessories.EnumerateArray())
                            if (accessory.TryGetProperty("selected", out var selected) &&
                                selected.ValueKind == System.Text.Json.JsonValueKind.False) continue;
                            else if (accessory.TryGetProperty("id", out var id) && id.GetString() is { } accessoryId)
                                accessoryIds.Add(accessoryId);
                }

                if (variantValues.Count != configuration.Options.Count || configuration.Options.Any(option =>
                        !variantValues.TryGetValue(option.Key, out var value) ||
                        option.Values.All(candidate => candidate.Key != value)))
                    return Conflict(new { code = "invalid-product-options" });
                var offer = catalog.Offers.Single(candidate =>
                    candidate.ProductId == quote.ProductId && candidate.SalesPlanId == quote.SalesPlanId);
                if (configuration.Accessories.Any(a => a.IsRequired && !accessoryIds.Contains(a.Key)) ||
                    accessoryIds.Any(id => configuration.Accessories.All(a => a.Key != id)) ||
                    offer.AccessoryIds is { } allowed &&
                    accessoryIds.Any(id => !allowed.Contains(id, StringComparer.Ordinal)))
                    return Conflict(new { code = "invalid-product-options" });
                decimal unitAmount = quote.UnitAmount;
                ProductInventoryVariant? selectedInventoryVariant = catalog.InventoryVariants.SingleOrDefault(variant =>
                    variant.ProductId == line.ProductId && variant.VariantId == line.VariantId);
                if (selectedInventoryVariant is not null &&
                    selectedInventoryVariant.PriceBySalesPlan.TryGetValue(line.SalesPlanId, out decimal variantPrice))
                {
                    decimal variantDiscount =
                        selectedInventoryVariant.DiscountBySalesPlan.GetValueOrDefault(line.SalesPlanId);
                    unitAmount = decimal.Round(variantPrice * (100m - variantDiscount) / 100m, 0,
                        MidpointRounding.AwayFromZero);
                }

                foreach (var option in configuration.Options)
                {
                    string selected = variantValues[option.Key];
                    unitAmount += option.Values.Single(v => v.Key == selected).PriceDelta;
                }

                foreach (var accessory in configuration.Accessories)
                    if (accessory.IsRequired || accessoryIds.Contains(accessory.Key))
                    {
                        if (!offer.AccessoryPriceAmounts.TryGetValue(accessory.Key, out var amount))
                            return Conflict(new { code = "invalid-product-options" });
                        unitAmount += amount;
                    }

                priced.Add((line, unitAmount, productName));
            }
        }
        catch (SalesPlanUnavailableException)
        {
            return Conflict(new { code = "price-or-stock-changed" });
        }

        decimal total = priced.Sum(x => checked(x.UnitAmount * x.Line.Quantity));
        int count = priced.Sum(x => x.Line.Quantity);
        string number = $"ND{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        List<long> insertedOrders = await db.Database.SqlQuery<long>(
                $"INSERT INTO [Account].[CustomerOrder]([IdProfile],[IdAddress],[OrderNumber],[PaymentMethod],[ItemCount],[TotalAmount],[AddressSnapshotJson]) OUTPUT INSERTED.[Id] AS [Value] VALUES({ProfileId},{request.AddressId},{number},{request.PaymentMethod},{count},{total},{addressSnapshot})")
            .ToListAsync(ct);
        long orderId = insertedOrders.Single();
        foreach (var item in priced)
        {
            string options = item.Line.Options.HasValue ? item.Line.Options.Value.GetRawText() : "{}";
            int insertedItems = await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO [Account].[CustomerOrderItem]([IdCustomerOrder],[IdProduct],[IdProductVariant],[ProductNameSnapshot],[IdSalesPlan],[Quantity],[UnitAmount],[OptionsJson]) SELECT {orderId},{item.Line.ProductId},{item.Line.VariantId},{item.ProductName},[Id],{item.Line.Quantity},{item.UnitAmount},{options} FROM [Store].[SalesPlan] WHERE [SalesPlanKey]={item.Line.SalesPlanId}",
                ct);
            if (insertedItems != 1) throw new InvalidOperationException("The sales plan no longer exists.");
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"IF NOT EXISTS(SELECT 1 FROM [Account].[CheckoutPreference] WHERE [IdProfile]={ProfileId}) INSERT INTO [Account].[CheckoutPreference]([IdProfile],[IdSelectedAddress],[PaymentMethod]) VALUES({ProfileId},{request.AddressId},{request.PaymentMethod}); ELSE UPDATE [Account].[CheckoutPreference] SET [IdSelectedAddress]={request.AddressId},[PaymentMethod]={request.PaymentMethod},[UpdatedAtUtc]=SYSUTCDATETIME() WHERE [IdProfile]={ProfileId}; DELETE ci FROM [Account].[CartItem] ci JOIN [Account].[Cart] c ON c.[Id]=ci.[IdCart] WHERE c.[IdProfile]={ProfileId};",
            ct);
        await transaction.CommitAsync(ct);
        return Ok(new
        {
            id = orderId, number, status = "pending-payment", itemCount = count, totalAmount = total,
            orderedAtUtc = DateTimeOffset.UtcNow, message = "سفارش ثبت شد و در انتظار پرداخت است."
        });
    }

    private async Task<bool> ProductVariantExistsAsync(long productId, long variantId,
        CancellationToken cancellationToken) =>
        await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM [Warehouse].[ProductVariant] v INNER JOIN [Warehouse].[Product] p ON p.[Id]=v.[IdProduct] WHERE p.[IdStoreProduct]={0} AND v.[Id]={1} AND v.[IsActive]=1 AND p.[IsActive]=1",
                productId, variantId)
            .SingleAsync(cancellationToken) == 1;

    [HttpDelete("addresses/{id:long}")]
    public async Task<IActionResult> DeleteAddress(long id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Account].[CheckoutPreference] SET [IdSelectedAddress]=NULL,[UpdatedAtUtc]=SYSUTCDATETIME() WHERE [IdProfile]={ProfileId} AND [IdSelectedAddress]={id};",
            ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Account].[CustomerOrder] SET [IdAddress]=NULL WHERE [IdProfile]={ProfileId} AND [IdAddress]={id};",
            ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [Account].[Address] WHERE [Id]={id} AND [IdProfile]={ProfileId}", ct);
        await transaction.CommitAsync(ct);
        return Ok(new { deleted = true });
    }

    [HttpPut("addresses/{id:long}")]
    public async Task<IActionResult> UpdateAddress(long id, AddressRequest a, CancellationToken ct)
    {
        if (id < 1 || new[]
            {
                a.Title, a.Recipient, a.Phone, a.Country, a.CountryId, a.Province, a.ProvinceId, a.City, a.CityId,
                a.Street, a.Postal
            }.Any(v => string.IsNullOrWhiteSpace(v)) ||
            a.Street!.Length > 500) return BadRequest(new { code = "invalid-address" });
        int changed = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Account].[Address] SET [Title]={a.Title!.Trim()},[Recipient]={a.Recipient!.Trim()},[Phone]={a.Phone!.Trim()},[Country]={a.Country!.Trim()},[CountryId]={a.CountryId!.Trim()},[Province]={a.Province!.Trim()},[ProvinceId]={a.ProvinceId!.Trim()},[City]={a.City!.Trim()},[CityId]={a.CityId!.Trim()},[Street]={a.Street!.Trim()},[PostalCode]={a.Postal!.Trim()} WHERE [Id]={id} AND [IdProfile]={ProfileId}",
            ct);
        return changed == 0 ? NotFound(new { code = "address-not-found" }) : Ok(new { saved = true, id });
    }

    [HttpPost("lists/{kind}/{productId:long}")]
    public async Task<IActionResult> ToggleList(string kind, long productId, CancellationToken ct)
    {
        if (kind is not ("favorites" or "compare") || productId < 1)
            return BadRequest(new { code = "invalid-list-item" });
        var changed = kind == "favorites"
            ? await db.Database.ExecuteSqlInterpolatedAsync(
                $"IF EXISTS(SELECT 1 FROM [Account].[Favorite] WHERE [IdProfile]={ProfileId} AND [IdProduct]={productId}) DELETE FROM [Account].[Favorite] WHERE [IdProfile]={ProfileId} AND [IdProduct]={productId}; ELSE INSERT INTO [Account].[Favorite]([IdProfile],[IdProduct]) SELECT {ProfileId},[Id] FROM [Store].[Product] WHERE [Id]={productId} AND [IsActive]=1;",
                ct)
            : await db.Database.ExecuteSqlInterpolatedAsync(
                $"IF EXISTS(SELECT 1 FROM [Account].[CompareItem] WHERE [IdProfile]={ProfileId} AND [IdProduct]={productId}) DELETE FROM [Account].[CompareItem] WHERE [IdProfile]={ProfileId} AND [IdProduct]={productId}; ELSE INSERT INTO [Account].[CompareItem]([IdProfile],[IdProduct]) SELECT {ProfileId},[Id] FROM [Store].[Product] WHERE [Id]={productId} AND [IsActive]=1;",
                ct);
        return Ok(new { changed = changed > 0 });
    }

    [HttpPut("lists/{kind}")]
    public async Task<IActionResult> SaveList(string kind, [FromBody] ListRequest request, CancellationToken ct)
    {
        if (kind is not ("favorites" or "compare") || request.ProductIds is null || request.ProductIds.Count > 100 ||
            request.ProductIds.Any(id => id < 1) ||
            request.ProductIds.Distinct().Count() != request.ProductIds.Count)
            return BadRequest(new { code = "invalid-list" });
        string json = System.Text.Json.JsonSerializer.Serialize(request.ProductIds);
        if (kind == "favorites")
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SET XACT_ABORT ON; BEGIN TRANSACTION; DELETE FROM [Account].[Favorite] WHERE [IdProfile]={ProfileId}; INSERT INTO [Account].[Favorite]([IdProfile],[IdProduct]) SELECT {ProfileId},p.[Id] FROM OPENJSON({json}) j JOIN [Store].[Product] p ON p.[Id]=TRY_CONVERT(BIGINT,j.[value]) AND p.[IsActive]=1; COMMIT;",
                ct);
        else
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SET XACT_ABORT ON; BEGIN TRANSACTION; DELETE FROM [Account].[CompareItem] WHERE [IdProfile]={ProfileId}; INSERT INTO [Account].[CompareItem]([IdProfile],[IdProduct]) SELECT {ProfileId},p.[Id] FROM OPENJSON({json}) j JOIN [Store].[Product] p ON p.[Id]=TRY_CONVERT(BIGINT,j.[value]) AND p.[IsActive]=1; COMMIT;",
                ct);
        return Ok(new { saved = true });
    }

    [HttpPut("price-alerts")]
    public async Task<IActionResult> SavePriceAlerts([FromBody] PriceAlertListRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count > 100)
            return BadRequest(new { code = "invalid-price-alerts" });
        var normalized = new List<object>();
        var configurations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in request.Items)
        {
            if (!long.TryParse(item.ProductId, out long productId) || productId < 1 ||
                item.Scope is not ("all" or "variant") || item.PriceAtRegistration < 0 ||
                (item.StockAtRegistration?.Length ?? 0) > 100 ||
                item.Scope == "variant" && item.Variants is null)
                return BadRequest(new { code = "invalid-price-alert" });
            string? variants = item.Variants is { ValueKind: System.Text.Json.JsonValueKind.Object } value
                ? value.GetRawText()
                : null;
            string signature = variants ?? "";
            if (!configurations.Add($"{productId}:{item.Scope}:{signature}"))
                return BadRequest(new { code = "duplicate-price-alert" });
            normalized.Add(new
            {
                productId, scope = item.Scope, signature, variantsJson = variants, item.PriceAtRegistration,
                item.StockAtRegistration, item.CreatedAt
            });
        }

        string payload = System.Text.Json.JsonSerializer.Serialize(normalized,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        await db.Database.ExecuteSqlInterpolatedAsync($$"""
                                                        SET XACT_ABORT ON; BEGIN TRANSACTION;
                                                        DELETE FROM [Account].[PriceAlert] WHERE [IdProfile]={{ProfileId}};
                                                        INSERT INTO [Account].[PriceAlert]([IdProfile],[IdProduct],[Scope],[VariantSignature],[VariantsJson],[PriceAtRegistration],[StockAtRegistration],[CreatedAtUtc])
                                                        SELECT {{ProfileId}},p.[Id],j.[Scope],j.[Signature],j.[VariantsJson],j.[PriceAtRegistration],j.[StockAtRegistration],COALESCE(j.[CreatedAt],SYSUTCDATETIME())
                                                        FROM OPENJSON({{payload}}) WITH([ProductId] BIGINT '$.productId',[Scope] NVARCHAR(20) '$.scope',[Signature] NVARCHAR(500) '$.signature',[VariantsJson] NVARCHAR(MAX) '$.variantsJson' AS JSON,[PriceAtRegistration] DECIMAL(19,0) '$.priceAtRegistration',[StockAtRegistration] NVARCHAR(100) '$.stockAtRegistration',[CreatedAt] DATETIME2(3) '$.createdAt') j
                                                        JOIN [Store].[Product] p ON p.[StoreProductKey]=CONVERT(NVARCHAR(128),j.[ProductId]) AND p.[IsActive]=1;
                                                        IF @@ROWCOUNT<>(SELECT COUNT(*) FROM OPENJSON({{payload}})) THROW 51002,'Price alert contains an unavailable product.',1;
                                                        COMMIT TRANSACTION;
                                                        """, ct);
        return Ok(new { saved = true });
    }

    [HttpPost("articles/{slug}/interactions/{type}")]
    public async Task<IActionResult> ToggleArticleInteraction(string slug, string type, CancellationToken ct)
    {
        if (slug.Length is < 1 or > 160 || !slug.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ||
            type is not ("like" or "bookmark" or "read")) return BadRequest(new { code = "invalid-interaction" });
        if (await db.Database
                .SqlQuery<int>(
                    $"SELECT COUNT(*) AS [Value] FROM [Mag].[MagazineArticle] WHERE [ArticleKey]={slug} AND [IsPublished]=1")
                .SingleAsync(ct) == 0) return NotFound(new { code = "article-not-found" });
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DECLARE @articleId BIGINT=(SELECT [Id] FROM [Mag].[MagazineArticle] WHERE [ArticleKey]={slug}); IF EXISTS(SELECT 1 FROM [Account].[ArticleInteraction] WHERE [IdProfile]={ProfileId} AND [IdMagazineArticle]=@articleId AND [InteractionType]={type}) DELETE FROM [Account].[ArticleInteraction] WHERE [IdProfile]={ProfileId} AND [IdMagazineArticle]=@articleId AND [InteractionType]={type}; ELSE INSERT INTO [Account].[ArticleInteraction]([IdProfile],[IdMagazineArticle],[InteractionType]) VALUES({ProfileId},@articleId,{type});",
            ct);
        return Ok(new { saved = true });
    }

    [HttpPost("articles/{slug}/feedback")]
    public async Task<IActionResult> AddArticleFeedback(string slug, [FromBody] ArticleFeedbackRequest request,
        CancellationToken ct)
    {
        string name = (request.Name ?? "").Trim();
        string text = (request.Text ?? "").Trim();
        if (slug.Length is < 1 or > 160 || !slug.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ||
            name.Length is < 3 or > 80 || name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2 ||
            text.Length is < 1 or > 1000 ||
            request.Rating is < 1 or > 5) return BadRequest(new { code = "invalid-feedback" });
        if (await db.Database
                .SqlQuery<int>(
                    $"SELECT COUNT(*) AS [Value] FROM [Mag].[MagazineArticle] WHERE [ArticleKey]={slug} AND [IsPublished]=1")
                .SingleAsync(ct) == 0) return NotFound(new { code = "article-not-found" });
        List<long> inserted = await db.Database.SqlQuery<long>(
                $"INSERT INTO [Account].[ArticleFeedback]([IdProfile],[IdMagazineArticle],[DisplayName],[FeedbackText],[Rating]) OUTPUT INSERTED.[Id] AS [Value] SELECT {ProfileId},[Id],{name},{text},{request.Rating} FROM [Mag].[MagazineArticle] WHERE [ArticleKey]={slug} AND [IsPublished]=1")
            .ToListAsync(ct);
        if (inserted.Count == 0) return NotFound(new { code = "article-not-found" });
        long id = inserted.Single();
        return Ok(new { saved = true, id, status = "pending" });
    }

    public sealed record ProfileRequest(
        string FirstName,
        string LastName,
        string? Phone,
        string? Gender = null,
        bool Newsletter = false);

    public sealed record CartRequest(IReadOnlyList<CartLineRequest>? Items);

    public sealed record CartLineRequest(
        long ProductId,
        int Quantity,
        string SalesPlanId,
        System.Text.Json.JsonElement? Options,
        long? VariantId = null);

    public sealed record CheckoutRequest(long AddressId, string PaymentMethod, IReadOnlyList<CartLineRequest>? Lines);

    public sealed record CheckoutPreferenceRequest(long? AddressId, string PaymentMethod);

    public sealed record ListRequest(IReadOnlyList<long>? ProductIds);

    public sealed record PriceAlertListRequest(IReadOnlyList<PriceAlertRequest>? Items);

    public sealed record PriceAlertRequest(
        string? ProductId,
        string? Scope,
        System.Text.Json.JsonElement? Variants,
        decimal PriceAtRegistration,
        string? StockAtRegistration,
        DateTimeOffset CreatedAt);

    public sealed record ArticleFeedbackRequest(string? Name, string? Text, byte? Rating);

    public sealed record RecentSearchRequest(string? Query);

    public sealed record AddressRequest(
        string? Title,
        string? Recipient,
        string? Phone,
        string? Country,
        string? CountryId,
        string? Province,
        string? ProvinceId,
        string? City,
        string? CityId,
        string? Street,
        string? Postal);
}