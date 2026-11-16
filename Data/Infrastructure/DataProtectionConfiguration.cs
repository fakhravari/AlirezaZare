using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shop.Data.Infrastructure;

public static class DataProtectionConfiguration
{
    public static IServiceCollection AddPersistentDataProtection(this IServiceCollection services, IConfiguration configuration, string contentRoot, string environment)
    {
        string? keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath))
            keysPath = Path.Combine(contentRoot, "App_Data", "keys");
        if (!Path.IsPathFullyQualified(keysPath)) throw new InvalidOperationException("DataProtection:KeysPath must be an absolute path.");

        services.AddDataProtection().SetApplicationName($"my-app-{environment}")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        return services;
    }
}
