namespace Shop.Data.Application.Store.Models;

public sealed record StoreSiteIdentitySnapshot(
    string DisplayName,
    string StoreName,
    string VisualBrandName,
    string SupportPhoneDisplay,
    string SupportPhoneUri,
    string WhatsAppNumber,
    string PublicEmail,
    string FooterDescription,
    string? InstagramUrl,
    string? TelegramUrl,
    string? LinkedInUrl);
