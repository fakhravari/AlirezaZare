namespace Shop.Database.Application.Store.Models;

public sealed record StoreSiteIdentitySnapshot(
    string DisplayName,
    string StoreName,
    string VisualBrandName,
    string SupportPhoneDisplay,
    string SupportPhoneUri,
    string WhatsAppNumber,
    string PublicEmail);
