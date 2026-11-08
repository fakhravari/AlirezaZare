using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shop.Database.Application.Warehouse.Contracts;
using Shop.Database.Application.Warehouse.Services;
using Shop.Database.Application.Store.Contracts;
using Shop.Database.Infrastructure.Persistence;
using Shop.Database.Infrastructure.Persistence.Repositories;

namespace Shop.Database.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShopDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<ShopDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IWarehouseReadRepository, WarehouseReadRepository>();
        services.AddScoped<IWarehouseReadService, WarehouseReadService>();
        services.AddScoped<IStoreSalesPlanRepository, StoreSalesPlanRepository>();
        services.AddScoped<IStoreRuntimeDatasetRepository, StoreRuntimeDatasetRepository>();
        services.AddScoped<IProductFeedbackRepository, StoreProductFeedbackRepository>();
        return services;
    }
}
