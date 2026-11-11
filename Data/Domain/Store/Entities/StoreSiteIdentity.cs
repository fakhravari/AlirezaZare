namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreSiteIdentity
{
    public byte Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string VisualBrandName { get; set; } = string.Empty;
    public string SupportPhoneDisplay { get; set; } = string.Empty;
    public string SupportPhoneUri { get; set; } = string.Empty;
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string PublicEmail { get; set; } = string.Empty;
    public string MediaCdnBaseUrl { get; set; } = "https://cdn.bs-shop.ir/";
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public bool? SmtpEnableSsl { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpFrom { get; set; }
    public string? SmtpPassword { get; set; }
    public long? IdWarehouse { get; set; }
    public string? FtpHost { get; set; }
    public int? FtpPort { get; set; }
    public string? FtpUsername { get; set; }
    public string? FtpPassword { get; set; }
    public string? FtpPasswordProtected { get; set; }
    public string? UpdatedByUserKey { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
