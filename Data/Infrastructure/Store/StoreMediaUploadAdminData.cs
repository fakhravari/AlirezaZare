using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreMediaUploadAdminData(ShopDbContext db) : IStoreMediaUploadAdminData
{
    public Task<bool> CountryExistsAsync(string code, CancellationToken ct) =>
        db.GeoCountries.AnyAsync(x => x.CountryCode == code, ct);
    public Task<bool> ArticleExistsAsync(string key, CancellationToken ct) =>
        db.MagazineArticles.AnyAsync(x => x.ArticleKey == key, ct);
    public Task<bool> BrandExistsAsync(string key, CancellationToken ct) =>
        db.StoreBrands.AnyAsync(x => x.BrandKey == key, ct);
    public Task<bool> BankExistsAsync(string code, CancellationToken ct) =>
        db.StoreBanks.AnyAsync(x => x.Code == code, ct);

    public async Task<bool> SaveUploadAsync(MediaUploadRegistration registration, CancellationToken ct)
    {
        if (registration.Kind == "slider")
            db.StoreSliders.Add(new StoreSlider
            {
                Title = registration.Title!, Url = registration.SliderUrl!, ImagePath = registration.Path,
                ShowOnMobile = registration.ShowOnMobile, ShowOnDesktop = registration.ShowOnDesktop
            });
        else if (registration.IsDocument)
            db.StoreProductAssets.Add(new StoreProductAsset
            {
                IdProduct = registration.ProductId!.Value, IdMediaType = ProductMediaTypes.Document,
                Title = registration.Title!, Path = registration.Path, CreatedAtUtc = DateTime.UtcNow
            });
        else if (registration.IsVideo)
        {
            decimal productId = registration.ProductId!.Value;
            bool hasImage = await db.StoreProductAssets.AnyAsync(x => x.IdProduct == productId &&
                x.IdMediaType == ProductMediaTypes.Image && x.IsDefault, ct);
            if (!hasImage) return false;
            int order = (await db.StoreProductAssets.Where(x => x.IdProduct == productId &&
                x.IdMediaType == ProductMediaTypes.Video).MaxAsync(x => (int?)x.SortOrder, ct) ?? 0) + 1;
            db.StoreProductAssets.Add(new StoreProductAsset
            {
                IdProduct = productId, IdMediaType = ProductMediaTypes.Video, Path = registration.Path,
                Title = registration.Title!, SortOrder = order, CreatedAtUtc = DateTime.UtcNow
            });
        }
        else if (!string.IsNullOrEmpty(registration.PaymentInstitutionCode))
        {
            var bank = await db.StoreBanks.SingleOrDefaultAsync(x => x.Code == registration.PaymentInstitutionCode, ct);
            if (bank is null) return false;
            bank.LogoPath = registration.Path;
        }
        else if (registration.Kind == "country")
        {
            var country = await db.GeoCountries.SingleOrDefaultAsync(x => x.CountryCode == registration.CountryCode, ct);
            if (country is null) return false;
            country.FlagPath = registration.Path;
        }
        else if (registration.Kind == "article")
        {
            var article = await db.MagazineArticles.SingleOrDefaultAsync(x => x.ArticleKey == registration.ArticleKey, ct);
            if (article is null) return false;
            article.HeroImagePath = registration.Path;
            article.ContentImagePath = registration.Path;
            article.UpdatedAtUtc = DateTime.UtcNow;
        }
        else if (!string.IsNullOrEmpty(registration.BrandKey))
        {
            var brand = await db.StoreBrands.SingleOrDefaultAsync(x => x.BrandKey == registration.BrandKey, ct);
            if (brand is null) return false;
            brand.LogoPath = registration.Path;
        }
        else return true;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<StoreSliderData>> ReadSlidersAsync(CancellationToken ct) =>
        await db.StoreSliders.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new StoreSliderData(x.Id, x.Title, x.Url, x.ImagePath, x.ShowOnMobile, x.ShowOnDesktop))
            .ToListAsync(ct);

    public async Task<bool> UpdateSliderAsync(long id, string title, string url, bool showOnMobile,
        bool showOnDesktop, CancellationToken ct)
    {
        var slide = await db.StoreSliders.FindAsync([id], ct);
        if (slide is null) return false;
        slide.Title = title;
        slide.Url = url;
        slide.ShowOnMobile = showOnMobile;
        slide.ShowOnDesktop = showOnDesktop;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteSliderAsync(long id, CancellationToken ct)
    {
        var slide = await db.StoreSliders.FindAsync([id], ct);
        if (slide is null) return false;
        db.StoreSliders.Remove(slide);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
