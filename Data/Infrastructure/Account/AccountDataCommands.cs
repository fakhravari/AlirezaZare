using System.Data;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Account.Contracts;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Account;

public sealed class AccountDataCommands(ShopDbContext db) : IAccountDataCommands
{
    public Task<bool> ProductVariantExistsAsync(decimal productId, long variantId, CancellationToken cancellationToken) =>
        db.ProductVariants.AsNoTracking().AnyAsync(variant => variant.Id == variantId &&
            variant.IdProduct == productId && variant.IsActive && variant.Product.IsStoreEnabled, cancellationToken);

    public Task<bool> GenderExistsAsync(int genderId, CancellationToken cancellationToken) =>
        db.AccountGenders.AsNoTracking().AnyAsync(gender => gender.Id == genderId, cancellationToken);

    public async Task<bool> UpdateProfileAsync(decimal profileId, AccountProfileUpdate update,
        CancellationToken cancellationToken)
    {
        var profile = await db.AccountProfiles.SingleOrDefaultAsync(item => item.Id == profileId, cancellationToken);
        if (profile is null) return false;
        profile.FirstName = update.FirstName;
        profile.LastName = update.LastName;
        profile.Phone = update.Phone;
        profile.IdGender = update.GenderId;
        profile.NewsletterEnabled = update.NewsletterEnabled;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<string?> ReadPasswordAsync(decimal profileId, CancellationToken cancellationToken) =>
        db.AccountProfiles.AsNoTracking().Where(item => item.Id == profileId).Select(item => item.Password)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> SetPasswordAsync(decimal profileId, string password, CancellationToken cancellationToken)
    {
        var profile = await db.AccountProfiles.SingleOrDefaultAsync(item => item.Id == profileId, cancellationToken);
        if (profile is null) return false;
        profile.Password = password;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> IsAddressLocationValidAsync(long countryId, long provinceId, long cityId,
        CancellationToken cancellationToken) => db.GeoCities.AsNoTracking().AnyAsync(city =>
        city.Id == cityId && city.IdProvince == provinceId && city.IdCountry == countryId &&
        db.GeoProvinces.AsNoTracking().Any(province => province.Id == city.IdProvince &&
            province.IdCountry == city.IdCountry), cancellationToken);

    public async Task<decimal> AddAddressAsync(decimal profileId, AccountAddressUpdate update,
        CancellationToken cancellationToken)
    {
        var address = new AccountAddress
        {
            IdProfile = profileId, Title = update.Title, Recipient = update.Recipient, Phone = update.Phone,
            IdCountry = update.CountryId, IdProvince = update.ProvinceId, IdCity = update.CityId,
            Street = update.Street, PostalCode = update.PostalCode, CreatedAtUtc = DateTime.UtcNow
        };
        db.AccountAddresses.Add(address);
        await db.SaveChangesAsync(cancellationToken);
        return address.Id;
    }

    public async Task<AccountAddressDeleteResult> DeleteAddressAsync(decimal profileId, long addressId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var address = await db.AccountAddresses.SingleOrDefaultAsync(item => item.Id == addressId &&
            item.IdProfile == profileId, cancellationToken);
        if (address is null) return AccountAddressDeleteResult.NotFound;
        if (await db.StoreCustomerOrders.AsNoTracking().AnyAsync(order => order.IdAddress == addressId,
                cancellationToken)) return AccountAddressDeleteResult.HasOrders;
        db.AccountAddresses.Remove(address);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AccountAddressDeleteResult.Deleted;
    }

    public async Task<bool> UpdateAddressAsync(decimal profileId, long addressId, AccountAddressUpdate update,
        CancellationToken cancellationToken)
    {
        var address = await db.AccountAddresses.SingleOrDefaultAsync(item => item.Id == addressId &&
            item.IdProfile == profileId, cancellationToken);
        if (address is null) return false;
        address.Title = update.Title;
        address.Recipient = update.Recipient;
        address.Phone = update.Phone;
        address.IdCountry = update.CountryId;
        address.IdProvince = update.ProvinceId;
        address.IdCity = update.CityId;
        address.Street = update.Street;
        address.PostalCode = update.PostalCode;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AccountMessageData>> ReadMessagesAsync(decimal profileId,
        CancellationToken cancellationToken) => await db.AccountMessages.AsNoTracking()
        .Where(item => item.IdProfile == profileId).OrderByDescending(item => item.CreatedAtUtc)
        .ThenByDescending(item => item.Id)
        .Select(item => new AccountMessageData(item.Id, item.Title, item.Category, item.Body, !item.IsRead,
            item.CreatedAtUtc)).ToListAsync(cancellationToken);

    public async Task<bool> MarkMessageReadAsync(decimal profileId, long messageId,
        CancellationToken cancellationToken)
    {
        var message = await db.AccountMessages.SingleOrDefaultAsync(item => item.Id == messageId &&
            item.IdProfile == profileId, cancellationToken);
        if (message is null) return false;
        if (!message.IsRead)
        {
            message.IsRead = true;
            message.ReadAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    public async Task<IReadOnlyList<string>> ReadRecentSearchesAsync(decimal profileId,
        CancellationToken cancellationToken) => await db.AccountRecentSearches.AsNoTracking()
        .Where(item => item.IdProfile == profileId).OrderByDescending(item => item.SearchedAtUtc)
        .ThenByDescending(item => item.Id).Select(item => item.SearchText).Take(6).ToListAsync(cancellationToken);

    public async Task SaveRecentSearchAsync(decimal profileId, string searchText, string searchKey,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var existing = await db.AccountRecentSearches.SingleOrDefaultAsync(item => item.IdProfile == profileId &&
            item.SearchKey == searchKey, cancellationToken);
        if (existing is null)
            db.AccountRecentSearches.Add(new AccountRecentSearch
            {
                IdProfile = profileId, SearchKey = searchKey, SearchText = searchText, SearchedAtUtc = DateTime.UtcNow
            });
        else
        {
            existing.SearchText = searchText;
            existing.SearchedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        var keep = await db.AccountRecentSearches.AsNoTracking().Where(item => item.IdProfile == profileId)
            .OrderByDescending(item => item.SearchedAtUtc).ThenByDescending(item => item.Id)
            .Select(item => item.Id).Take(6).ToListAsync(cancellationToken);
        await db.AccountRecentSearches.Where(item => item.IdProfile == profileId && !keep.Contains(item.Id))
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task ClearRecentSearchesAsync(decimal profileId, CancellationToken cancellationToken) =>
        db.AccountRecentSearches.Where(item => item.IdProfile == profileId).ExecuteDeleteAsync(cancellationToken);

    public async Task<bool?> ToggleListItemAsync(decimal profileId, string kind, long productId,
        CancellationToken cancellationToken)
    {
        if (!await db.Products.AsNoTracking().AnyAsync(item => item.Id == productId && item.IsStoreEnabled,
                cancellationToken)) return null;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        bool changed;
        if (kind == "favorites")
        {
            var existing = await db.AccountFavorites.SingleOrDefaultAsync(item => item.IdProfile == profileId &&
                item.IdProduct == productId, cancellationToken);
            changed = existing is null;
            if (existing is null) db.AccountFavorites.Add(new AccountFavorite
                { IdProfile = profileId, IdProduct = productId, CreatedAtUtc = DateTime.UtcNow });
            else db.AccountFavorites.Remove(existing);
        }
        else
        {
            var existing = await db.AccountCompareItems.SingleOrDefaultAsync(item => item.IdProfile == profileId &&
                item.IdProduct == productId, cancellationToken);
            changed = existing is null;
            if (existing is null) db.AccountCompareItems.Add(new AccountCompareItem
                { IdProfile = profileId, IdProduct = productId, CreatedAtUtc = DateTime.UtcNow });
            else db.AccountCompareItems.Remove(existing);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return changed;
    }

    public async Task<bool> ReplaceListAsync(decimal profileId, string kind, IReadOnlyList<decimal> productIds,
        CancellationToken cancellationToken)
    {
        int available = await db.Products.AsNoTracking().CountAsync(item => productIds.Contains(item.Id) &&
            item.IsStoreEnabled, cancellationToken);
        if (available != productIds.Count) return false;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        if (kind == "favorites")
        {
            await db.AccountFavorites.Where(item => item.IdProfile == profileId).ExecuteDeleteAsync(cancellationToken);
            db.AccountFavorites.AddRange(productIds.Select(id => new AccountFavorite
                { IdProfile = profileId, IdProduct = id, CreatedAtUtc = DateTime.UtcNow }));
        }
        else
        {
            await db.AccountCompareItems.Where(item => item.IdProfile == profileId).ExecuteDeleteAsync(cancellationToken);
            db.AccountCompareItems.AddRange(productIds.Select(id => new AccountCompareItem
                { IdProfile = profileId, IdProduct = id, CreatedAtUtc = DateTime.UtcNow }));
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReplacePriceAlertsAsync(decimal profileId, IReadOnlyList<AccountPriceAlertData> alerts,
        CancellationToken cancellationToken)
    {
        decimal[] productIds = alerts.Select(item => (decimal)item.ProductId).Distinct().ToArray();
        int available = await db.Products.AsNoTracking().CountAsync(item => productIds.Contains(item.Id) &&
            item.IsStoreEnabled, cancellationToken);
        if (available != productIds.Length) return false;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        await db.AccountPriceAlerts.Where(item => item.IdProfile == profileId).ExecuteDeleteAsync(cancellationToken);
        db.AccountPriceAlerts.AddRange(alerts.Select(item => new AccountPriceAlert
        {
            IdProfile = profileId, IdProduct = item.ProductId, Scope = item.Scope,
            VariantSignature = item.VariantSignature, VariantsJson = item.VariantsJson,
            PriceAtRegistration = item.PriceAtRegistration, StockAtRegistration = item.StockAtRegistration,
            CreatedAtUtc = item.CreatedAtUtc
        }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ToggleArticleInteractionAsync(decimal profileId, string slug, string type,
        CancellationToken cancellationToken)
    {
        var articleId = await db.MagazineArticles.AsNoTracking().Where(article => article.ArticleKey == slug &&
            article.IsPublished).Select(article => (decimal?)article.Id).SingleOrDefaultAsync(cancellationToken);
        if (articleId is null) return false;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var existing = await db.AccountArticleInteractions.SingleOrDefaultAsync(item => item.IdProfile == profileId &&
            item.IdMagazineArticle == articleId.Value && item.InteractionType == type, cancellationToken);
        if (existing is null) db.AccountArticleInteractions.Add(new AccountArticleInteraction
            { IdProfile = profileId, IdMagazineArticle = articleId.Value, InteractionType = type });
        else db.AccountArticleInteractions.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<decimal?> AddArticleFeedbackAsync(decimal profileId, string slug, string name, string text,
        byte rating, CancellationToken cancellationToken)
    {
        var articleId = await db.MagazineArticles.AsNoTracking().Where(article => article.ArticleKey == slug &&
            article.IsPublished).Select(article => (decimal?)article.Id).SingleOrDefaultAsync(cancellationToken);
        if (articleId is null) return null;
        var feedback = new AccountArticleFeedback
        {
            IdProfile = profileId, IdMagazineArticle = articleId.Value, DisplayName = name, FeedbackText = text,
            Rating = rating,
            IdStatus = await db.ArticleFeedbackStatuses.AsNoTracking().Where(status => status.Code == "pending")
                .Select(status => status.Id).SingleAsync(cancellationToken),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AccountArticleFeedback.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);
        return feedback.Id;
    }

    public async Task<IReadOnlyList<AccountCartAccessoryData>> ReadCartAccessoriesAsync(decimal productId,
        CancellationToken cancellationToken) => await db.StoreProductAccessories.AsNoTracking()
        .Join(db.StoreAccessories.AsNoTracking(), link => link.IdAccessory, accessory => accessory.Id,
            (link, accessory) => new { link, accessory })
        .Where(row => row.link.IdProduct == productId && row.accessory.IsActive)
        .Select(row => new AccountCartAccessoryData(row.link.Id, row.accessory.Id, row.link.IsRequired))
        .ToListAsync(cancellationToken);

    public async Task<bool> SaveCartAsync(decimal profileId, IReadOnlyList<AccountCartLineData> lines,
        CancellationToken cancellationToken)
    {
        decimal[] productIds = lines.Select(line => line.ProductId).Distinct().ToArray();
        string[] planKeys = lines.Select(line => line.SalesPlanKey).Distinct(StringComparer.Ordinal).ToArray();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var availableProducts = await db.Products.AsNoTracking()
            .Where(product => productIds.Contains(product.Id) && product.IsStoreEnabled).Select(product => product.Id)
            .ToHashSetAsync(cancellationToken);
        var planIds = await db.SalesPlanReferences.AsNoTracking()
            .Where(plan => planKeys.Contains(plan.StoreSalesPlanKey) && plan.IsActive && plan.IdStockOwnerType == 1)
            .ToDictionaryAsync(plan => plan.StoreSalesPlanKey, plan => plan.Id, cancellationToken);
        if (availableProducts.Count != productIds.Length || planIds.Count != planKeys.Length) return false;

        foreach (var line in lines)
        {
            var accessoryLinks = await ReadCartAccessoriesAsync(line.ProductId, cancellationToken);
            if (line.AccessoryIds.Any(id => accessoryLinks.All(link => link.ProductAccessoryId != id)) ||
                accessoryLinks.Any(link => link.Required && !line.AccessoryIds.Contains(link.ProductAccessoryId)))
                return false;
        }

        var cart = await db.AccountCarts.SingleOrDefaultAsync(item => item.IdProfile == profileId,
            cancellationToken);
        if (cart is null)
        {
            cart = new AccountCart { IdProfile = profileId, UpdatedAtUtc = DateTime.UtcNow };
            db.AccountCarts.Add(cart);
            await db.SaveChangesAsync(cancellationToken);
        }
        await db.AccountCartItems.Where(item => item.IdCart == cart.Id).ExecuteDeleteAsync(cancellationToken);
        DateTime now = DateTime.UtcNow;
        var savedItems = lines.Select(line => new AccountCartItem
        {
            IdCart = cart.Id, IdProduct = line.ProductId, IdVariant = line.VariantId,
            IdSalesPlan = planIds[line.SalesPlanKey], Quantity = line.Quantity, UpdatedAtUtc = now
        }).ToArray();
        db.AccountCartItems.AddRange(savedItems);
        await db.SaveChangesAsync(cancellationToken);
        for (int index = 0; index < lines.Count; index++)
            db.AccountCartItemAccessories.AddRange(lines[index].AccessoryIds.Select(id =>
                new AccountCartItemAccessory { IdCartItem = savedItems[index].Id, IdProductAccessory = id }));
        cart.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
