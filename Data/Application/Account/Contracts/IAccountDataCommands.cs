namespace Shop.Data.Application.Account.Contracts;

public interface IAccountDataCommands
{
    Task<bool> ProductVariantExistsAsync(decimal productId, long variantId, CancellationToken cancellationToken);
    Task<bool> GenderExistsAsync(int genderId, CancellationToken cancellationToken);
    Task<bool> UpdateProfileAsync(decimal profileId, AccountProfileUpdate update, CancellationToken cancellationToken);
    Task<string?> ReadPasswordAsync(decimal profileId, CancellationToken cancellationToken);
    Task<bool> SetPasswordAsync(decimal profileId, string password, CancellationToken cancellationToken);
    Task<bool> IsAddressLocationValidAsync(long countryId, long provinceId, long cityId,
        CancellationToken cancellationToken);
    Task<decimal> AddAddressAsync(decimal profileId, AccountAddressUpdate address, CancellationToken cancellationToken);
    Task<AccountAddressDeleteResult> DeleteAddressAsync(decimal profileId, long addressId,
        CancellationToken cancellationToken);
    Task<bool> UpdateAddressAsync(decimal profileId, long addressId, AccountAddressUpdate address,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountMessageData>> ReadMessagesAsync(decimal profileId, CancellationToken cancellationToken);
    Task<bool> MarkMessageReadAsync(decimal profileId, long messageId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ReadRecentSearchesAsync(decimal profileId, CancellationToken cancellationToken);
    Task SaveRecentSearchAsync(decimal profileId, string searchText, string searchKey,
        CancellationToken cancellationToken);
    Task ClearRecentSearchesAsync(decimal profileId, CancellationToken cancellationToken);
    Task<bool?> ToggleListItemAsync(decimal profileId, string kind, long productId,
        CancellationToken cancellationToken);
    Task<bool> ReplaceListAsync(decimal profileId, string kind, IReadOnlyList<decimal> productIds,
        CancellationToken cancellationToken);
    Task<bool> ReplacePriceAlertsAsync(decimal profileId, IReadOnlyList<AccountPriceAlertData> alerts,
        CancellationToken cancellationToken);
    Task<bool> ToggleArticleInteractionAsync(decimal profileId, string slug, string type,
        CancellationToken cancellationToken);
    Task<decimal?> AddArticleFeedbackAsync(decimal profileId, string slug, string name, string text, byte rating,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountCartAccessoryData>> ReadCartAccessoriesAsync(decimal productId,
        CancellationToken cancellationToken);
    Task<bool> SaveCartAsync(decimal profileId, IReadOnlyList<AccountCartLineData> lines,
        CancellationToken cancellationToken);
}

public sealed record AccountProfileUpdate(string FirstName, string LastName, string? Phone, int? GenderId,
    bool NewsletterEnabled);
public sealed record AccountAddressUpdate(string Title, string Recipient, string Phone, long CountryId,
    long ProvinceId, long CityId, string Street, string PostalCode);
public enum AccountAddressDeleteResult { NotFound, HasOrders, Deleted }
public sealed record AccountMessageData(long Id, string Title, string Category, string Body, bool Unread,
    DateTime CreatedAtUtc);
public sealed record AccountPriceAlertData(long ProductId, string Scope, string VariantSignature,
    string? VariantsJson, decimal PriceAtRegistration, string? StockAtRegistration, DateTime CreatedAtUtc);
public sealed record AccountCartAccessoryData(decimal ProductAccessoryId, decimal AccessoryId, bool Required);
public sealed record AccountCartLineData(decimal ProductId, string SalesPlanKey, long? VariantId, int Quantity,
    IReadOnlyList<decimal> AccessoryIds);
