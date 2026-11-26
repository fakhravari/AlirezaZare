using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreSiteSettingsAdminData(ShopDbContext db) : IStoreSiteSettingsAdminData
{
    public async Task<StoreSiteSettingsData?> ReadAsync(CancellationToken cancellationToken)
    {
        var x = await db.StoreSiteIdentities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        return x is null ? null : new StoreSiteSettingsData
        {
            DisplayName = x.DisplayName, StoreName = x.StoreName, VisualBrandName = x.VisualBrandName,
            SupportPhoneDisplay = x.SupportPhoneDisplay, SupportPhoneUri = x.SupportPhoneUri,
            WhatsAppNumber = x.WhatsAppNumber, PublicEmail = x.PublicEmail, MediaCdnBaseUrl = x.MediaCdnBaseUrl,
            FooterDescription = x.FooterDescription, InstagramUrl = x.InstagramUrl, TelegramUrl = x.TelegramUrl,
            LinkedInUrl = x.LinkedInUrl, IdWarehouse = x.IdWarehouse, SmtpHost = x.SmtpHost ?? "",
            SmtpPort = x.SmtpPort, SmtpEnableSsl = x.SmtpEnableSsl ?? false, SmtpUsername = x.SmtpUsername ?? "",
            SmtpFrom = x.SmtpFrom ?? "", SmtpPassword = x.SmtpPassword,
            EmailVerificationCodeLifetimeMinutes = x.EmailVerificationCodeLifetimeMinutes, FtpHost = x.FtpHost,
            FtpPort = x.FtpPort, FtpUsername = x.FtpUsername, FtpPassword = x.FtpPassword,
            LegacyFtpPassword = x.FtpPasswordProtected,
            FtpPasswordConfigured = x.FtpPassword is not null || x.FtpPasswordProtected is not null
        };
    }

    public async Task<IReadOnlyList<StoreSalesWarehouseData>> ReadSalesWarehousesAsync(
        CancellationToken cancellationToken) => await db.Warehouses.AsNoTracking()
        .Where(w => w.IsActive && w.IsSalesWarehouse && w.Company.IsActive)
        .OrderBy(w => w.Company.Name).ThenBy(w => w.Name)
        .Select(w => new StoreSalesWarehouseData(w.Id, w.Company.Name + " / " + w.Name + " (" + w.Code + ")"))
        .ToListAsync(cancellationToken);

    public async Task<StoreSettingsSaveResult> SaveAsync(StoreSiteSettingsData model, string updatedByUserKey,
        CancellationToken cancellationToken)
    {
        if (model.IdWarehouse.HasValue && !await db.Warehouses.AnyAsync(w => w.Id == model.IdWarehouse &&
                w.IsActive && w.IsSalesWarehouse && w.Company.IsActive, cancellationToken))
            return StoreSettingsSaveResult.InvalidWarehouse;
        var settings = await db.StoreSiteIdentities.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (settings is null) return StoreSettingsSaveResult.NotFound;
        settings.DisplayName = model.DisplayName; settings.StoreName = model.StoreName;
        settings.VisualBrandName = model.VisualBrandName; settings.SupportPhoneDisplay = model.SupportPhoneDisplay;
        settings.SupportPhoneUri = model.SupportPhoneUri; settings.WhatsAppNumber = model.WhatsAppNumber;
        settings.PublicEmail = model.PublicEmail; settings.MediaCdnBaseUrl = model.MediaCdnBaseUrl;
        settings.IdWarehouse = model.IdWarehouse; settings.FooterDescription = model.FooterDescription;
        settings.InstagramUrl = model.InstagramUrl; settings.TelegramUrl = model.TelegramUrl;
        settings.LinkedInUrl = model.LinkedInUrl; settings.SmtpHost = model.SmtpHost;
        settings.SmtpPort = model.SmtpPort; settings.SmtpEnableSsl = model.SmtpEnableSsl;
        settings.SmtpUsername = model.SmtpUsername; settings.SmtpFrom = model.SmtpFrom;
        if (model.SmtpPassword is not null) settings.SmtpPassword = model.SmtpPassword;
        settings.EmailVerificationCodeLifetimeMinutes = model.EmailVerificationCodeLifetimeMinutes;
        settings.FtpHost = model.FtpHost; settings.FtpPort = model.FtpPort; settings.FtpUsername = model.FtpUsername;
        if (model.FtpPassword is not null)
        {
            settings.FtpPassword = model.FtpPassword;
            settings.FtpPasswordProtected = null;
        }
        settings.UpdatedAtUtc = DateTime.UtcNow;
        settings.UpdatedByUserKey = updatedByUserKey;
        await db.SaveChangesAsync(cancellationToken);
        return StoreSettingsSaveResult.Saved;
    }
}
