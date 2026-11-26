namespace Shop.Data.Application.Store.Contracts;

public interface IStoreMediaUploadAdminData
{
    Task<bool> CountryExistsAsync(string code, CancellationToken cancellationToken);
    Task<bool> ArticleExistsAsync(string key, CancellationToken cancellationToken);
    Task<bool> BrandExistsAsync(string key, CancellationToken cancellationToken);
    Task<bool> BankExistsAsync(string code, CancellationToken cancellationToken);
    Task<bool> SaveUploadAsync(MediaUploadRegistration registration, CancellationToken cancellationToken);
    Task<IReadOnlyList<StoreSliderData>> ReadSlidersAsync(CancellationToken cancellationToken);
    Task<bool> UpdateSliderAsync(long id, string title, string url, bool showOnMobile, bool showOnDesktop,
        CancellationToken cancellationToken);
    Task<bool> DeleteSliderAsync(long id, CancellationToken cancellationToken);
}

public sealed record MediaUploadRegistration(string Kind, string Path, decimal? ProductId, string? Title,
    string? SliderUrl, bool ShowOnMobile, bool ShowOnDesktop, string? BrandKey, string? CountryCode,
    string? ArticleKey, string? PaymentInstitutionCode, bool IsDocument, bool IsVideo);
public sealed record StoreSliderData(long Id, string Title, string Url, string ImagePath,
    bool ShowOnMobile, bool ShowOnDesktop);
