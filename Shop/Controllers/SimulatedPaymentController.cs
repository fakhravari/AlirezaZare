using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
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
        var order = await ReadOrderAsync(id, ct);
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
        var order = await ReadOrderAsync(id, ct);
        if (order is null) return NotFound();
        if (order.Status is not ("pending-payment" or "simulated-payment-confirmed")) return Conflict();
        if (result == "approved" && order.Status == "pending-payment")
            await db.StoreCustomerOrders
                .Where(x => x.Id == id && x.IdProfile == ProfileId && x.Status == "pending-payment")
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "simulated-payment-confirmed"), ct);
        return RedirectToAction(nameof(Index), new { id, cancelled = result == "cancelled" && order.Status == "pending-payment" });
    }

    private Task<SimulatedPaymentOrder?> ReadOrderAsync(long id, CancellationToken ct) =>
        db.StoreCustomerOrders.AsNoTracking()
            .Where(x => x.Id == id && x.IdProfile == ProfileId)
            .Select(x => new SimulatedPaymentOrder(x.Id, x.OrderNumber, x.TotalAmount, x.Status))
            .SingleOrDefaultAsync(ct);
}

public sealed record SimulatedPaymentOrder(long Id, string Number, decimal Amount, string Status);

public sealed record SimulatedPaymentPage(SimulatedPaymentOrder Order, bool Cancelled);
