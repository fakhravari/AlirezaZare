using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shop.Admin.Controllers.Api;
using Shop.Controllers;
using Shop.Database.Infrastructure.Persistence;
using Shop.Models;

internal static class ServicePagesMainVerification
{
    public static async Task RunAsync(string connectionString)
    {
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(connection.InitialCatalog, "bsshopir_fakhravari", StringComparison.OrdinalIgnoreCase) ||
            !connection.DataSource.Contains("31.25.91.7\\sqlserver2022", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Service-page verification is restricted to the configured primary database.");

        var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connectionString).Options;
        await using var db = new ShopDbContext(options);
        string migration = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "Shop", "Features", "Store", "Database", "Migrations", "051-service-page-content.sql"));
        await db.Database.ExecuteSqlRawAsync(migration);
        string correction = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "Shop", "Features", "Store", "Database", "Migrations", "052-correct-service-page-privacy-content.sql"));
        await db.Database.ExecuteSqlRawAsync(correction);

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var api = new StoreServicePagesAdminApiController(db);
            var listed = (await api.List(default) as OkObjectResult)?.Value as IEnumerable<object>;
            if (listed is null || listed.Count() < 9) throw new InvalidOperationException("Service-page list is incomplete.");
            if ((await api.Get("privacy", default) as OkObjectResult)?.Value is not StoreServicePagesAdminApiController.PageInput privacy || privacy.Sections is null ||
                !privacy.Sections.Any(x => x.Body is { } body && body.Contains("localStorage", StringComparison.Ordinal) && body.Contains("API", StringComparison.Ordinal)))
                throw new InvalidOperationException("Privacy content does not match current account storage behavior.");
            if ((await api.Get("about", default) as OkObjectResult)?.Value is not StoreServicePagesAdminApiController.PageInput initial ||
                initial.Sections is not { Count: > 0 }) throw new InvalidOperationException("Seeded service-page content is missing.");

            string suffix = Guid.NewGuid().ToString("N");
            var edit = new StoreServicePagesAdminApiController.PageInput("عنوان آزمایشی " + suffix, "شرح آزمایشی", true,
                [new("verification", "بخش آزمایشی", "متن آزمایشی", true), new("verification-2", "بخش دوم", "متن دوم", true)]);
            if (await api.Update("about", edit, default) is not OkObjectResult) throw new InvalidOperationException("Service-page update failed.");
            var stored = await db.StoreContentPages.AsNoTracking().Include(x => x.Sections).SingleAsync(x => x.PageKey == "about");
            if (stored.Title != edit.Title || stored.Sections.Count != 2 || stored.Sections.OrderBy(x => x.SortOrder).First().Body != "متن آزمایشی")
                throw new InvalidOperationException("Service-page changes did not persist in EF.");
            if (await api.Update("about", edit with { Sections = [new("duplicate", "الف", "متن"), new("duplicate", "ب", "متن")] }, default) is not BadRequestObjectResult)
                throw new InvalidOperationException("Duplicate section keys were accepted.");

            var services = new ServiceCollection();
            services.AddSingleton(db);
            services.AddSingleton<IOptions<SiteIdentityOptions>>(Options.Create(new SiteIdentityOptions { DisplayName = "آزمایش", StoreName = "آزمایش" }));
            using var provider = services.BuildServiceProvider();
            var httpContext = new DefaultHttpContext { RequestServices = provider };
            var probe = new ServicePageProbe { ControllerContext = new ControllerContext { HttpContext = httpContext }, TempData = new TempDataDictionary(httpContext, new EmptyTempDataProvider()) };
            if (await probe.Load("about") is not ViewResult view || view.ViewName != "~/Views/Shared/ManagedContentPage.cshtml" ||
                view.Model is not PageModel { ManagedContent.Title: var renderedTitle } || renderedTitle != edit.Title)
                throw new InvalidOperationException("Public service page did not render edited database content.");

            if (await api.Update("about", edit with { IsPublished = false }, default) is not OkObjectResult ||
                await probe.Load("about") is not NotFoundResult)
                throw new InvalidOperationException("Unpublished service page remained publicly available.");
            Console.WriteLine("Primary service-page seed, EF editing, section ordering/validation, public rendering and unpublish checks passed; temporary edits rolled back.");
        }
        finally { await transaction.RollbackAsync(); }
    }

    private sealed class ServicePageProbe : PageController
    {
        public Task<IActionResult> Load(string key) => PageAsync(new PageModel(new SeoModel("عنوان", "شرح")) { ContentKey = key });
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
