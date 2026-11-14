namespace Shop.Models;

public sealed class SiteIdentityOptions
{
    public string DisplayName { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string VisualBrandName { get; set; } = string.Empty;
    public string SupportPhoneDisplay { get; set; } = string.Empty;
    public string SupportPhoneUri { get; set; } = string.Empty;
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string PublicEmail { get; set; } = string.Empty;
    public string FooterDescription { get; set; } = string.Empty;
    public string? InstagramUrl { get; set; }
    public string? TelegramUrl { get; set; }
    public string? LinkedInUrl { get; set; }
}
