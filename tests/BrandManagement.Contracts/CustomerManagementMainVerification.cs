using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Controllers.Api;
using Shop.Data.Infrastructure.Persistence;

internal static class CustomerManagementMainVerification
{
    public static async Task Run(string connectionString)
    {
        var connection = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(connection.InitialCatalog, "bsshopir_fakhravari", StringComparison.OrdinalIgnoreCase) ||
            !connection.DataSource.Contains("31.25.91.7\\sqlserver2022", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Customer-management verification is restricted to the configured primary SQL database.");

        var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connectionString).Options;
        await using var db = new ShopDbContext(options);
        var controller = new StoreCustomersAdminApiController(db);
        string suffix = Guid.NewGuid().ToString("N");
        string email = $"verify-{suffix}@example.invalid";
        long id = 0;
        try
        {
            var create = new StoreCustomersAdminApiController.CustomerCreate(email, "مشتری", "آزمایشی", "09120000000");
            if (await controller.Create(create, default) is not OkObjectResult created) throw new InvalidOperationException("Customer creation failed.");
            id = (long)created.Value!.GetType().GetProperty("id")!.GetValue(created.Value)!;
            var details = (await controller.Details(id, default) as OkObjectResult)?.Value as StoreCustomersAdminApiController.CustomerDetails;
            if (details is null || details.Email != email) throw new InvalidOperationException("Created customer could not be read.");

            var update = new StoreCustomersAdminApiController.CustomerUpdate($"edited-{suffix}@example.invalid", "نام ویرایش‌شده", "آزمایشی", null,
                details.Email, details.FirstName, details.LastName, details.Phone);
            if (await controller.Update(id, update, default) is not OkObjectResult) throw new InvalidOperationException("Customer update failed.");
            if (await controller.Update(id, update, default) is not ConflictObjectResult) throw new InvalidOperationException("Stale customer update was accepted.");
            if (await controller.Update(id, update with { Email = "not-an-email" }, default) is not BadRequestObjectResult) throw new InvalidOperationException("Invalid email was accepted.");
            if (await controller.Create(create with { Email = "invalid" }, default) is not BadRequestObjectResult) throw new InvalidOperationException("Invalid customer email was accepted.");

            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Account].[Cart] ([IdProfile]) VALUES ({id})");
            if (await controller.Delete(id, default) is not ConflictObjectResult) throw new InvalidOperationException("Customer with an active cart was deleted.");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Account].[Cart] WHERE [IdProfile]={id}");
            if (await controller.Delete(id, default) is not OkObjectResult) throw new InvalidOperationException("Unreferenced customer could not be deleted.");
            id = 0;
            if (await db.AccountProfiles.AnyAsync(x => x.Email == update.Email)) throw new InvalidOperationException("Deleted customer remains in the database.");
            Console.WriteLine("Primary customer create, details, email/profile edit, validation, stale-write protection, cart deletion guard and safe delete checks passed.");
        }
        finally
        {
            if (id != 0)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Account].[Cart] WHERE [IdProfile]={id}");
                await db.AccountProfiles.Where(x => x.Id == id).ExecuteDeleteAsync();
            }
        }
    }
}
