using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shop.Data.Application.Account.Contracts;
using Shop.Data.Application.Admin.Contracts;
using Shop.Data.Application.Email.Contracts;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Application.Warehouse.Services;
using Shop.Data.Infrastructure.Email;
using Shop.Data.Infrastructure.Account;
using Shop.Data.Infrastructure.Admin;
using Shop.Data.Infrastructure.Identity;
using Shop.Data.Infrastructure.Media;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Infrastructure.Persistence.Repositories;
using Shop.Data.Infrastructure.Store;
using Shop.Data.Infrastructure.Warehouse;

namespace Shop.Data.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAdminDataOperations(this IServiceCollection services)
    {
        services.AddScoped<IAdminUsersData, AdminUsersDataService>();
        services.AddScoped<IWarehouseMovementsAdminData, WarehouseMovementsAdminData>();
        services.AddScoped<IWarehouseDefinitionsAdminData, WarehouseDefinitionsAdminData>();
        services.AddScoped<IWarehouseVouchersAdminData, WarehouseVouchersAdminData>();
        services.AddScoped<IStoreCatalogAdminData, StoreCatalogAdminData>();
        services.AddScoped<IStoreVariantPricesAdminData, StoreVariantPricesAdminData>();
        return services;
    }

    public static IServiceCollection AddShopDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<ShopDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailVerificationCodeStore, DatabaseEmailVerificationCodeStore>();
        services.AddScoped<IAccountDataCommands, AccountDataCommands>();
        services.AddScoped<IAdminIdentityData, AdminIdentityData>();
        services.AddScoped<ISystemProblemsData, SystemProblemsDataService>();
        services.AddScoped<IWarehouseStockLedger, WarehouseStockLedger>();
        services.AddScoped<IVoucherNumberGenerator, VoucherNumberGenerator>();

        string mediaCdnBaseUrl =
            configuration[$"{MediaCdnOptions.SectionName}:BaseUrl"] ?? MediaCdnOptions.DefaultBaseUrl;
        services.AddOptions<MediaCdnOptions>().Configure(options => options.BaseUrl = mediaCdnBaseUrl)
            .Validate(options =>
            {
                try
                {
                    MediaCdnOptions.NormalizeBaseUrl(options.BaseUrl);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }, "MediaCdn:BaseUrl must be a valid HTTPS origin.").ValidateOnStart();

        services.AddScoped<IWarehouseReadRepository, WarehouseReadRepository>();
        services.AddScoped<IWarehouseReadService, WarehouseReadService>();
        services.AddScoped<IWarehouseInventoryAdminService, WarehouseInventoryAdminService>();
        services.AddScoped<IStorePaidOrderDispatchData, StorePaidOrderDispatchData>();
        services.AddScoped<IStoreSalesPlanRepository, StoreSalesPlanRepository>();
        services.AddScoped<IStoreRuntimeDatasetRepository, StoreRuntimeDatasetRepository>();
        services.AddScoped<IStorePublicPageData, StorePublicPageData>();
        services.AddScoped<IStoreNewsletterAdminData, StoreNewsletterAdminData>();
        services.AddScoped<IStoreContentAdminData, StoreContentAdminData>();
        services.AddScoped<IStoreProductBadgeAdminData, StoreProductBadgeAdminData>();
        services.AddScoped<IStoreAccountMessageAdminData, StoreAccountMessageAdminData>();
        services.AddScoped<IStoreContactMessageAdminData, StoreContactMessageAdminData>();
        services.AddScoped<IStoreMagazineAdminData, StoreMagazineAdminData>();
        services.AddScoped<IStoreHomeCollectionAdminData, StoreHomeCollectionAdminData>();
        services.AddScoped<IStoreSiteSettingsAdminData, StoreSiteSettingsAdminData>();
        services.AddScoped<IStoreCouponAdminData, StoreCouponAdminData>();
        services.AddScoped<IStoreHomeTestimonialAdminData, StoreHomeTestimonialAdminData>();
        services.AddScoped<IStoreProductImageAdminData, StoreProductImageAdminData>();
        services.AddScoped<IStoreCustomersAdminData, StoreCustomersAdminData>();
        services.AddScoped<IStoreGeographyAdminData, StoreGeographyAdminData>();
        services.AddScoped<IMediaCdnBaseUrlProvider, StoreMediaCdnBaseUrlProvider>();
        services.AddScoped<IStoreSiteIdentityRepository, StoreSiteIdentityRepository>();
        services.AddScoped<IProductFeedbackRepository, StoreProductFeedbackRepository>();
        services.AddScoped<IStorePriceVersionService, StorePriceVersionService>();
        services.AddScoped<IStoreCatalogFeaturesAdminData, StoreCatalogFeaturesAdminData>();
        services.AddScoped<IStoreMediaUploadAdminData, StoreMediaUploadAdminData>();
        services.AddScoped<IStoreOrdersAdminData, StoreOrdersAdminData>();
        services.AddScoped<IStoreReportsAdminData, StoreReportsAdminData>();

        return services;
    }
}
