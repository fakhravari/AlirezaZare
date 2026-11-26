using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;

namespace Shop.Data.Infrastructure.Persistence.Repositories;

public sealed class StorePublicPageData(ShopDbContext dbContext) : IStorePublicPageData
{
    public async Task<IReadOnlyList<PaymentInstitutionData>> ReadPaymentInstitutionsAsync(
        CancellationToken cancellationToken) => await dbContext.StoreBanks
        .AsNoTracking()
        .Where(bank => bank.IsActive)
        .OrderBy(bank => bank.Id)
        .Select(bank => new PaymentInstitutionData(bank.NameFa, bank.LogoPath))
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentInstitutionAdminData>> ReadPaymentInstitutionAdminRowsAsync(
        CancellationToken cancellationToken) => await dbContext.StoreBanks.AsNoTracking().OrderBy(bank => bank.Id)
        .Select(bank => new PaymentInstitutionAdminData(bank.Id, bank.Code, bank.NameFa, bank.LogoPath, bank.IsActive))
        .ToListAsync(cancellationToken);

    public async Task<ManagedPageData?> ReadPublishedPageAsync(string pageKey, CancellationToken cancellationToken)
    {
        var page = await dbContext.StoreContentPages
            .AsNoTracking()
            .Where(item => item.PageKey == pageKey && item.IsPublished)
            .Select(item => new
            {
                item.Id,
                item.PageKey,
                item.RoutePath,
                item.Title,
                item.Description,
                Sections = item.Sections.Where(section => section.IsPublished)
                    .OrderBy(section => section.SortOrder).ThenBy(section => section.Id)
                    .Select(section => new ManagedPageSectionData(section.Id, section.SectionKey, section.Heading,
                        section.Body, section.SortOrder)).ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);
        return page is null
            ? null
            : new ManagedPageData(page.Id, page.PageKey, page.RoutePath, page.Title, page.Description, page.Sections);
    }

    public async Task<IReadOnlyList<OrderTrackingData>> FindOrderTrackingCandidatesAsync(string orderNumber,
        CancellationToken cancellationToken) => await dbContext.StoreCustomerOrders
        .AsNoTracking()
        .Where(order => order.OrderNumber == orderNumber || dbContext.StoreOrderPreviousNumbers
            .AsNoTracking().Any(previous => previous.IdOrder == order.Id && previous.PreviousNumber == orderNumber))
        .Select(order => new OrderTrackingData(order.Id, order.OrderNumber, order.StatusReference.Code,
            order.StatusReference.NameFa, order.CreatedAtUtc, order.ShippingCarrier, order.ShippingTrackingCode,
            order.AddressSnapshotJson))
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OrderTrackingHistoryData>> ReadOrderTrackingHistoryAsync(decimal orderId,
        CancellationToken cancellationToken) => await dbContext.StoreOrderShipmentTrackingHistories
        .AsNoTracking()
        .Where(history => history.IdOrder == orderId)
        .OrderBy(history => history.ChangedAtUtc)
        .Select(history => new OrderTrackingHistoryData(history.Carrier, history.TrackingCode, history.ChangedAtUtc))
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AssignedCouponData>> ReadAssignedCouponsAsync(long profileId,
        CancellationToken cancellationToken) => await dbContext.AccountCoupons
        .AsNoTracking()
        .Where(coupon => coupon.IdProfile == profileId)
        .Select(coupon => new AssignedCouponData(coupon.CouponKey, coupon.Code, coupon.DiscountPercent, coupon.Enabled,
            coupon.ExpiresOn, coupon.UsageLimit))
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProfileGenderData>> ReadProfileGendersAsync(CancellationToken cancellationToken) =>
        await dbContext.AccountGenders.AsNoTracking().OrderBy(gender => gender.Id)
            .Select(gender => new ProfileGenderData(gender.Id, gender.Code, gender.NameFa))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HomeSliderData>> ReadHomeSlidersAsync(CancellationToken cancellationToken) =>
        await dbContext.StoreSliders.AsNoTracking()
            .Where(slider => slider.ShowOnMobile || slider.ShowOnDesktop).OrderBy(slider => slider.Id)
            .Select(slider => new HomeSliderData(slider.ShowOnMobile, slider.ShowOnDesktop, slider.Url,
                slider.ImagePath, slider.Title)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HomeTestimonialData>> ReadHomeTestimonialsAsync(
        CancellationToken cancellationToken) => await dbContext.StoreHomeTestimonials.AsNoTracking()
        .Where(testimonial => testimonial.IsActive)
        .OrderBy(testimonial => testimonial.SortOrder)
        .ThenByDescending(testimonial => testimonial.CreatedAtUtc)
        .ThenByDescending(testimonial => testimonial.Id)
        .Select(testimonial => new HomeTestimonialData(testimonial.DisplayName, testimonial.Product.NameFa,
            testimonial.FeedbackText, testimonial.Rating, testimonial.CreatedAtUtc, testimonial.ImagePath))
        .ToListAsync(cancellationToken);

    public async Task<PasswordCredentialData?> ReadPasswordCredentialAsync(string email,
        CancellationToken cancellationToken) => await dbContext.AccountProfiles.AsNoTracking()
        .Where(profile => profile.Email == email)
        .Select(profile => new PasswordCredentialData(profile.Id, profile.Password))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<int> ReadEmailVerificationLifetimeMinutesAsync(CancellationToken cancellationToken) =>
        await dbContext.StoreSiteIdentities.AsNoTracking().Where(identity => identity.Id == 1)
            .Select(identity => identity.EmailVerificationCodeLifetimeMinutes).SingleOrDefaultAsync(cancellationToken);

    public async Task<VerifiedAccountData> EnsureVerifiedAccountAsync(string email, string? password,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.AccountProfiles.SingleOrDefaultAsync(item => item.Email == email,
            cancellationToken);
        if (profile is null)
        {
            profile = new() { Email = email, CreatedAtUtc = DateTime.UtcNow };
            dbContext.AccountProfiles.Add(profile);
        }

        if (password is not null) profile.Password = password;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new VerifiedAccountData(profile.Id);
    }

    public async Task<bool> EnableAccountNewsletterAsync(long profileId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.AccountProfiles.SingleOrDefaultAsync(item => item.Id == profileId,
            cancellationToken);
        if (profile is null) return false;
        profile.NewsletterEnabled = true;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SubscribeNewsletterAsync(string email, CancellationToken cancellationToken)
    {
        if (await dbContext.StoreNewsletterSubscribers.AsNoTracking().AnyAsync(item => item.Email == email,
                cancellationToken)) return false;
        dbContext.StoreNewsletterSubscribers.Add(new StoreNewsletterSubscriber
            { Email = email, SubscribedAtUtc = DateTime.UtcNow });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            if (!await dbContext.StoreNewsletterSubscribers.AsNoTracking().AnyAsync(item => item.Email == email,
                    cancellationToken)) throw;
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<decimal> SaveContactMessageAsync(string name, string phone, string? email, string subject,
        string message, CancellationToken cancellationToken)
    {
        var contactMessage = new StoreContactMessage
        {
            Name = name,
            Phone = phone,
            Email = email,
            SubjectKey = subject,
            Message = message,
            IdStatus = await dbContext.ContactMessageStatuses.AsNoTracking().Where(status => status.Code == "new")
                .Select(status => status.Id).SingleAsync(cancellationToken),
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.StoreContactMessages.Add(contactMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
        return contactMessage.Id;
    }

    public async Task<IReadOnlyList<PriceHistoryData>?> ReadPriceHistoryAsync(int productId, string salesPlanKey,
        long? variantId, CancellationToken cancellationToken)
    {
        long? planId = await dbContext.SalesPlanReferences.AsNoTracking()
            .Where(plan => plan.IdStockOwnerType == 1 && plan.StoreSalesPlanKey == salesPlanKey)
            .Select(plan => (long?)plan.Id).SingleOrDefaultAsync(cancellationToken);
        if (!planId.HasValue) return null;
        return await dbContext.StorePriceVersions.AsNoTracking()
            .Where(version => version.IdProduct == productId && version.IdSalesPlan == planId.Value &&
                              version.IdVariant == variantId && version.IdAccessory == null)
            .OrderBy(version => version.CreatedAtUtc).ThenBy(version => version.Id)
            .Select(version => new PriceHistoryData(version.Id, version.IdVariant, version.IdAccessory,
                version.PriceBeforeDiscountAmount, version.DiscountPercent, version.FinalPriceAmount,
                version.IsActive, version.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
