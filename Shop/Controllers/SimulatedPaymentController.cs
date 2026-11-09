using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;
using Shop.Models;

namespace Shop.Controllers;

[Authorize(AuthenticationSchemes = "ShopCookie")]
[Route("payment/simulated")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SimulatedPaymentController(ShopDbContext db, IWebHostEnvironment environment) : PageController
{
    private long ProfileId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Index(long id, bool cancelled = false, CancellationToken ct = default)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var order = (await db.Database.SqlQueryRaw<SimulatedPaymentOrder>(
            "SELECT [Id], [OrderNumber] AS [Number], [TotalAmount] AS [Amount], [Status] FROM [Store].[Order] WHERE [Id]={0} AND [IdProfile]={1}",
            id, ProfileId).ToListAsync(ct)).SingleOrDefault();
        if (order is null) return NotFound();
        return await PageAsync(new PageModel<SimulatedPaymentPage>(
            new(order, cancelled), new SeoModel("پرداخت آزمایشی", "شبیه‌سازی پرداخت سفارش", Robots: "noindex, nofollow"), "Index"));
    }

    [HttpPost("{id:long}/return")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(long id, string? result, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return NotFound();
        if (result is not ("approved" or "cancelled")) return BadRequest();
        var orders = await db.Database.SqlQueryRaw<SimulatedPaymentOrder>(
            "SELECT [Id], [OrderNumber] AS [Number], [TotalAmount] AS [Amount], [Status] FROM [Store].[Order] WHERE [Id]={0} AND [IdProfile]={1}",
            id, ProfileId).ToListAsync(ct);
        var order = orders.SingleOrDefault();
        if (order is null) return NotFound();
        if (order.Status is not ("pending-payment" or "simulated-payment-confirmed")) return Conflict();
        if (result == "approved" && order.Status == "pending-payment")
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [Store].[Order] SET [Status]={"simulated-payment-confirmed"} WHERE [Id]={id} AND [IdProfile]={ProfileId} AND [Status]={"pending-payment"}", ct);
        return RedirectToAction(nameof(Index), new { id, cancelled = result == "cancelled" && order.Status == "pending-payment" });
    }
}

public sealed class SimulatedPaymentOrder
{
    public long Id { get; set; }
    public string Number { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
}

public sealed record SimulatedPaymentPage(SimulatedPaymentOrder Order, bool Cancelled);
