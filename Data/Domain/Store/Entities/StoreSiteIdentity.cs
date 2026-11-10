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
    public DateTime UpdatedAtUtc { get; set; }
}
