using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Application.Warehouse.Services;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Application.Store;
using Shop.Data.Application.Email.Contracts;
using Shop.Data.Infrastructure.Email;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Infrastructure.Persistence.Repositories;

namespace Shop.Data.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShopDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<ShopDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        string mediaCdnBaseUrl = configuration[$"{MediaCdnOptions.SectionName}:BaseUrl"] ?? MediaCdnOptions.DefaultBaseUrl;
        services.AddOptions<MediaCdnOptions>()
            .Configure(options => options.BaseUrl = mediaCdnBaseUrl)
            .Validate(options => { try { MediaCdnOptions.NormalizeBaseUrl(options.BaseUrl); return true; } catch (InvalidOperationException) { return false; } }, "MediaCdn:BaseUrl must be a valid HTTPS origin.")
            .ValidateOnStart();
        services.AddScoped<IWarehouseReadRepository, WarehouseReadRepository>();
        services.AddScoped<IWarehouseReadService, WarehouseReadService>();
        services.AddScoped<IStoreSalesPlanRepository, StoreSalesPlanRepository>();
        services.AddScoped<IStoreRuntimeDatasetRepository, StoreRuntimeDatasetRepository>();
        services.AddScoped<IMediaCdnBaseUrlProvider, StoreMediaCdnBaseUrlProvider>();
        services.AddScoped<IStoreSiteIdentityRepository, StoreSiteIdentityRepository>();
        services.AddScoped<IProductFeedbackRepository, StoreProductFeedbackRepository>();
        return services;
    }
}
