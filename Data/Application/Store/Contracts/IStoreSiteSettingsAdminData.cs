namespace Shop.Data.Application.Store.Contracts;

public interface IStoreSiteSettingsAdminData
{
    Task<StoreSiteSettingsData?> ReadAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<StoreSalesWarehouseData>> ReadSalesWarehousesAsync(CancellationToken cancellationToken);
    Task<StoreSettingsSaveResult> SaveAsync(StoreSiteSettingsData settings, string updatedByUserKey,
        CancellationToken cancellationToken);
}

public sealed class StoreSiteSettingsData
{
    public string DisplayName { get; set; } = "";
    public string StoreName { get; set; } = "";
    public string VisualBrandName { get; set; } = "";
    public string SupportPhoneDisplay { get; set; } = "";
    public string SupportPhoneUri { get; set; } = "";
    public string WhatsAppNumber { get; set; } = "";
    public string PublicEmail { get; set; } = "";
    public string MediaCdnBaseUrl { get; set; } = "";
    public string FooterDescription { get; set; } = "";
    public string? InstagramUrl { get; set; }
    public string? TelegramUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public long? IdWarehouse { get; set; }
    public string SmtpHost { get; set; } = "";
    public int? SmtpPort { get; set; }
    public bool SmtpEnableSsl { get; set; }
    public string SmtpUsername { get; set; } = "";
    public string SmtpFrom { get; set; } = "";
    public string? SmtpPassword { get; set; }
    public int EmailVerificationCodeLifetimeMinutes { get; set; }
    public string? FtpHost { get; set; }
    public int? FtpPort { get; set; }
    public string? FtpUsername { get; set; }
    public string? FtpPassword { get; set; }
    public string? LegacyFtpPassword { get; set; }
    public bool FtpPasswordConfigured { get; set; }
}

public sealed record StoreSalesWarehouseData(long Id, string Text);
public enum StoreSettingsSaveResult { Saved, NotFound, InvalidWarehouse }
