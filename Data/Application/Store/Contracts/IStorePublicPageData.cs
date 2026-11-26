namespace Shop.Data.Application.Store.Contracts;

public interface IStorePublicPageData
{
    Task<IReadOnlyList<PaymentInstitutionData>> ReadPaymentInstitutionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentInstitutionAdminData>> ReadPaymentInstitutionAdminRowsAsync(CancellationToken cancellationToken);
    Task<ManagedPageData?> ReadPublishedPageAsync(string pageKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderTrackingData>> FindOrderTrackingCandidatesAsync(decimal orderId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AssignedCouponData>> ReadAssignedCouponsAsync(long profileId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ProfileGenderData>> ReadProfileGendersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<HomeSliderData>> ReadHomeSlidersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<HomeTestimonialData>> ReadHomeTestimonialsAsync(CancellationToken cancellationToken);
    Task<PasswordCredentialData?> ReadPasswordCredentialAsync(string email, CancellationToken cancellationToken);
    Task<int> ReadEmailVerificationLifetimeMinutesAsync(CancellationToken cancellationToken);
    Task<VerifiedAccountData> EnsureVerifiedAccountAsync(string email, string? password,
        CancellationToken cancellationToken);
    Task<bool> EnableAccountNewsletterAsync(long profileId, CancellationToken cancellationToken);
    Task<bool> SubscribeNewsletterAsync(string email, CancellationToken cancellationToken);
    Task<decimal> SaveContactMessageAsync(string name, string phone, string? email, string subject, string message,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceHistoryData>?> ReadPriceHistoryAsync(int productId, string salesPlanKey,
        long? variantId, CancellationToken cancellationToken);
}

public sealed record PaymentInstitutionData(string Name, string? LogoPath);
public sealed record PaymentInstitutionAdminData(long Id, string Code, string Name, string? LogoPath, bool IsActive);
public sealed record ManagedPageData(long Id, string PageKey, string RoutePath, string Title, string? Description,
    IReadOnlyList<ManagedPageSectionData> Sections);
public sealed record ManagedPageSectionData(long Id, string SectionKey, string? Heading, string? Body, int SortOrder);
public sealed record OrderTrackingData(decimal Id, string StatusNameEn, string StatusName,
    DateTime CreatedAtUtc, string? ShippingCarrier, string? ShippingTrackingCode, string Phone);
public sealed record AssignedCouponData(string Id, string Code, int DiscountPercent, bool Enabled, DateOnly ExpiresOn,
    int UsageLimit);
public sealed record ProfileGenderData(int Id, string Code, string Name);
public sealed record HomeSliderData(bool ShowOnMobile, bool ShowOnDesktop, string Url, string ImagePath, string Title);
public sealed record HomeTestimonialData(string DisplayName, string ProductName, string FeedbackText, int Rating,
    DateTime CreatedAtUtc, string? ImagePath);
public sealed record PasswordCredentialData(decimal ProfileId, string? Password);
public sealed record VerifiedAccountData(decimal ProfileId);
public sealed record PriceHistoryData(decimal Id, long? VariantId, decimal? AccessoryId, decimal PriceAmount,
    decimal DiscountPercent, decimal FinalPriceAmount, bool IsActive, DateTime CreatedAtUtc);
