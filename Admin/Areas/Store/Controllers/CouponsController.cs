using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Areas.Store.Controllers;

[Area("Store")]
[Authorize(Roles = "system.full-access")]
public sealed class CouponsController(ShopDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "کدهای تخفیف";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "coupons";
        q = (q ?? "").Trim().ToUpperInvariant();
        status = (status ?? "").Trim().ToLowerInvariant();
        if (q.Length > 32 || page is < 1 or > 100000 || status is not ("" or "active" or "disabled" or "expired" or "exhausted")) return BadRequest();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = db.AccountCoupons.AsNoTracking().Where(item => q == "" || item.Code.Contains(q));
        query = status switch
        {
            "disabled" => query.Where(item => !item.Enabled),
            "expired" => query.Where(item => item.Enabled && item.ExpiresOn < today),
            "exhausted" => query.Where(item => item.Enabled && item.ExpiresOn >= today && db.StoreCouponUsages.Count(usage => usage.IdCoupon == item.Id) >= item.UsageLimit),
            "active" => query.Where(item => item.Enabled && item.ExpiresOn >= today && db.StoreCouponUsages.Count(usage => usage.IdCoupon == item.Id) < item.UsageLimit),
            _ => query
        };
        int total = await query.CountAsync(cancellationToken);
        int uses = await db.StoreCouponUsages.CountAsync(usage => query.Any(item => item.Id == usage.IdCoupon), cancellationToken);
        decimal discount = await db.StoreCustomerOrders.Where(order => order.IdCoupon != null && query.Any(item => item.Id == order.IdCoupon))
            .SumAsync(order => (decimal?)order.CouponDiscountAmount, cancellationToken) ?? 0;
        var coupons = await query.OrderByDescending(item => item.Id).Skip((page - 1) * 25).Take(25)
            .Select(item => new CouponRow(item.Id, item.Code, item.DiscountPercent, item.UsageLimit, item.Enabled, item.ExpiresOn,
                db.StoreCouponUsages.Count(usage => usage.IdCoupon == item.Id),
                db.StoreCustomerOrders.Where(order => order.IdCoupon == item.Id).Sum(order => (decimal?)order.CouponDiscountAmount) ?? 0,
                db.StoreCustomerOrders.Any(order => order.IdCoupon == item.Id))).ToListAsync(cancellationToken);
        return View(new CouponIndexModel(coupons, q, status, page, total, uses, discount, today));
    }

    [HttpGet]
    public async Task<IActionResult> Report(long id, int page = 1, CancellationToken cancellationToken = default)
    {
        if (id <= 0 || page is < 1 or > 100000) return BadRequest();
        var coupon = await db.AccountCoupons.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (coupon is null) return NotFound();
        ViewData["Title"] = "گزارش مصرف کد تخفیف";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "coupons";
        var orders = from usage in db.StoreCouponUsages.AsNoTracking()
                     join order in db.StoreCustomerOrders.AsNoTracking() on usage.IdCustomerOrder equals order.Id
                     join profile in db.AccountProfiles.AsNoTracking() on order.IdProfile equals profile.Id
                     join orderStatus in db.OrderStatuses.AsNoTracking() on order.IdStatus equals orderStatus.Id
                     where usage.IdCoupon == id
                     select new CouponOrderRow(order.Id, order.OrderNumber, profile.FirstName + " " + profile.LastName,
                         orderStatus.NameFa, order.OrderedAtUtc, order.SubtotalAmount, order.CouponDiscountAmount, order.TotalAmount);
        int total = await orders.CountAsync(cancellationToken);
        decimal discount = await orders.SumAsync(order => (decimal?)order.Discount, cancellationToken) ?? 0;
        decimal subtotal = await orders.SumAsync(order => (decimal?)order.Subtotal, cancellationToken) ?? 0;
        decimal payable = await orders.SumAsync(order => (decimal?)order.Total, cancellationToken) ?? 0;
        var rows = await orders.OrderByDescending(order => order.OrderedAtUtc).ThenByDescending(order => order.Id)
            .Skip((page - 1) * 25).Take(25).ToListAsync(cancellationToken);
        return View(new CouponReportModel(coupon.Id, coupon.Code, coupon.UsageLimit, total, subtotal, discount, payable, page, rows));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var coupon = await db.AccountCoupons.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (coupon is null) return NotFound();
        if (await db.StoreCouponUsages.AnyAsync(usage => usage.IdCoupon == id, cancellationToken) ||
            await db.StoreCustomerOrders.AnyAsync(order => order.IdCoupon == id, cancellationToken))
        {
            TempData["CouponError"] = "این کوپن سابقهٔ سفارش دارد و حذف نمی‌شود؛ می‌توانید آن را غیرفعال کنید.";
            return RedirectToAction(nameof(Index));
        }
        db.AccountCoupons.Remove(coupon);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        TempData["CouponMessage"] = "کد تخفیف حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(long? id, string? code, int discountPercent, int usageLimit, DateOnly expiresOn, bool enabled, CancellationToken cancellationToken)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        if (!ModelState.IsValid || id is <= 0 || code.Length is < 1 or > 32 || code.Any(character => !char.IsAsciiLetterOrDigit(character)) ||
            discountPercent is < 1 or > 99 || usageLimit < 1 || expiresOn == default)
        {
            TempData["CouponError"] = "کد انگلیسی و عددی، درصد ۱ تا ۹۹، سقف مثبت و تاریخ معتبر وارد کنید.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var item = id is null ? null : await db.AccountCoupons.SingleOrDefaultAsync(coupon => coupon.Id == id, cancellationToken);
        if (id is not null && item is null) return NotFound();
        if (await db.AccountCoupons.AnyAsync(coupon => coupon.Code == code && coupon.Id != id, cancellationToken))
        {
            TempData["CouponError"] = "این کد تخفیف قبلاً تعریف شده است.";
            return RedirectToAction(nameof(Index));
        }
        int uses = item is null ? 0 : await db.StoreCouponUsages.CountAsync(usage => usage.IdCoupon == item.Id, cancellationToken);
        if (item is not null && item.Code != code && (uses > 0 || await db.StoreCustomerOrders.AnyAsync(order => order.IdCoupon == item.Id, cancellationToken)))
        {
            TempData["CouponError"] = "کد کوپن مصرف‌شده برای حفظ سابقهٔ سفارش قابل تغییر نیست.";
            return RedirectToAction(nameof(Index));
        }
        if (usageLimit < uses)
        {
            TempData["CouponError"] = "سقف استفاده نمی‌تواند کمتر از تعداد مصرف ثبت‌شده باشد.";
            return RedirectToAction(nameof(Index));
        }
        if (item is null)
        {
            item = new AccountCoupon { CouponKey = Guid.NewGuid().ToString("N") };
            db.AccountCoupons.Add(item);
        }
        item.Code = code;
        item.DiscountPercent = discountPercent;
        item.UsageLimit = usageLimit;
        item.ExpiresOn = expiresOn;
        item.Enabled = enabled;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        TempData["CouponMessage"] = "کد تخفیف ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }
}

public sealed record CouponRow(long Id, string Code, int DiscountPercent, int UsageLimit, bool Enabled, DateOnly ExpiresOn, int Uses, decimal Discount, bool HasOrders);
public sealed record CouponIndexModel(List<CouponRow> Coupons, string Query, string Status, int Page, int Total, int Uses, decimal Discount, DateOnly Today);
public sealed record CouponOrderRow(long Id, string OrderNumber, string Customer, string Status, DateTime OrderedAtUtc, decimal Subtotal, decimal Discount, decimal Total);
public sealed record CouponReportModel(long Id, string Code, int UsageLimit, int Uses, decimal Subtotal, decimal Discount, decimal Total, int Page, List<CouponOrderRow> Orders);
