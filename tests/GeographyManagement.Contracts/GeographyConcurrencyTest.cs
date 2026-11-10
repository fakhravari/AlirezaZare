using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Controllers.Api;
using Shop.Data.Infrastructure.Persistence;

internal static class GeographyConcurrencyTest
{
    public static async Task RunAsync(string connection)
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connection).Options;
        string suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(), codeA = "A" + suffix, codeB = "B" + suffix, label = "Geography race " + suffix;
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<IActionResult> Create(string code)
        {
            await using var context = new ShopDbContext(options);
            await start.Task;
            return await new StoreGeographyAdminApiController(context).Create("countries", new(label, label, code, null, null), default);
        }
        try
        {
            var one = Create(codeA); var two = Create(codeB); start.SetResult();
            var results = await Task.WhenAll(one, two);
            if (results.Any(result => result is not OkObjectResult)) throw new Exception("Concurrent country creation failed.");
            await using var verify = new ShopDbContext(options);
            int count = await verify.Database.SqlQuery<int>($"SELECT COUNT(DISTINCT [Id]) AS [Value] FROM [Geo].[Country] WHERE [NameFa]={label} AND [CountryCode] IN ({codeA},{codeB})").SingleAsync();
            if (count != 2) throw new Exception("Concurrent countries did not get distinct IDs.");
            Console.WriteLine("Concurrent geography creation passed.");
        }
        finally
        {
            await using var cleanup = new ShopDbContext(options);
            await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Geo].[Country] WHERE [NameFa]={label} AND [CountryCode] IN ({codeA},{codeB})");
        }
    }
}
