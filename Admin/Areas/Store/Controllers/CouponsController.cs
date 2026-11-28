using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers;

[Area("Store")]
[Authorize(Roles = "system.full-access")]
public sealed class CouponsController(IStoreCouponAdminData data) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, int page = 1,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "کدهای تخفیف";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "coupons";
        q = (q ?? "").Trim().ToUpperInvariant();
        status = (status ?? "").Trim().ToLowerInvariant();
        if (q.Length > 254 || page is < 1 or > 100000 ||
            status is not ("" or "active" or "disabled" or "expired" or "exhausted")) return BadRequest();
        var model = await ReadIndexModel(q, status, page, cancellationToken);
        return IsAjaxRequest() ? PartialView("_CouponContent", model) : View(model);
    }

    private async Task<CouponIndexModel> ReadIndexModel(string q, string status, int page, CancellationToken ct)
    {
        var result = await data.ReadAsync(q, status, page, DateOnly.FromDateTime(DateTime.UtcNow), ct);
        return new CouponIndexModel(result.Coupons.Select(x => new CouponRow(x.Id, x.Code, x.DiscountPercent,
            x.UsageLimit, x.Enabled, x.ExpiresOn, x.Uses, x.Discount, x.HasOrders, x.IdProfile, x.CustomerEmail)).ToList(),
            result.Query, result.Status, result.Page, result.Total, result.Uses, result.Discount, result.Today);
    }

    private bool IsAjaxRequest() => string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> Report(decimal id, int page = 1, CancellationToken cancellationToken = default)
    {
        if (id <= 0 || page is < 1 or > 100000) return BadRequest();
        var report = await data.ReadReportAsync(id, page, cancellationToken);
        if (report is null) return NotFound();
        ViewData["Title"] = "گزارش مصرف کد تخفیف";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "coupons";
        var model = new CouponReportModel(report.Id, report.Code, report.UsageLimit, report.Uses, report.Subtotal,
            report.Discount, report.Total, report.Page,
            report.Orders.Select(x => new CouponOrderRow(x.Id, x.OrderNumber, x.Customer, x.Status, x.CreatedAtUtc,
                x.Subtotal, x.Discount, x.Total)).ToList());
        return IsAjaxRequest() ? PartialView("_CouponReport", model) : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(decimal id, CancellationToken cancellationToken)
    {
        if (id <= 0) return BadRequest();
        var result = await data.DeleteAsync(id, cancellationToken);
        if (result == CouponMutationResult.NotFound) return IsAjaxRequest() ? BadRequest(new { success = false, message = "کد تخفیف پیدا نشد." }) : NotFound();
        if (result == CouponMutationResult.HasHistory)
        {
            if (IsAjaxRequest()) return BadRequest(new { success = false, message = "این کوپن سابقهٔ سفارش دارد و حذف نمی‌شود؛ می‌توانید آن را غیرفعال کنید." });
            TempData["CouponError"] = "این کوپن سابقهٔ سفارش دارد و حذف نمی‌شود؛ می‌توانید آن را غیرفعال کنید.";
            return RedirectToAction(nameof(Index));
        }
        if (IsAjaxRequest()) return Json(new { success = true, message = "کد تخفیف حذف شد." });
        TempData["CouponMessage"] = "کد تخفیف حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(long? id, string? code, int discountPercent, int usageLimit,
        DateOnly expiresOn, bool enabled, string? audience, string? customerEmail, CancellationToken cancellationToken)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        if (expiresOn == default)
        {
            const string message = "تاریخ انقضا را انتخاب کنید.";
            if (IsAjaxRequest()) return BadRequest(new { success = false, message });
            TempData["CouponError"] = message;
            return RedirectToAction(nameof(Index));
        }
        if (!ModelState.IsValid || id is <= 0 || code.Length is < 1 or > 32 ||
            discountPercent is < 1 or > 99 || usageLimit < 1 ||
            audience is not ("public" or "personal"))
        {
            if (IsAjaxRequest()) return BadRequest(new { success = false, message = "کد غیرخالی تا ۳۲ نویسه، درصد ۱ تا ۹۹، سقف مثبت و تاریخ معتبر وارد کنید." });
            TempData["CouponError"] = "کد غیرخالی تا ۳۲ نویسه، درصد ۱ تا ۹۹، سقف مثبت و تاریخ معتبر وارد کنید.";
            return RedirectToAction(nameof(Index));
        }

        var result = await data.SaveAsync(id, code, discountPercent, usageLimit, expiresOn, enabled, audience,
            customerEmail, cancellationToken);
        switch (result)
        {
            case CouponMutationResult.NotFound: return IsAjaxRequest() ? BadRequest(new { success = false, message = "کد تخفیف پیدا نشد." }) : NotFound();
            case CouponMutationResult.CustomerNotFound:
                if (IsAjaxRequest()) return BadRequest(new { success = false, message = "کاربری با ایمیل واردشده پیدا نشد." });
                TempData["CouponError"] = "کاربری با ایمیل واردشده پیدا نشد."; return RedirectToAction(nameof(Index));
            case CouponMutationResult.DuplicateCode:
                if (IsAjaxRequest()) return BadRequest(new { success = false, message = "این کد تخفیف قبلاً تعریف شده است." });
                TempData["CouponError"] = "این کد تخفیف قبلاً تعریف شده است."; return RedirectToAction(nameof(Index));
            case CouponMutationResult.UsedCode:
                if (IsAjaxRequest()) return BadRequest(new { success = false, message = "کد کوپن مصرف‌شده برای حفظ سابقهٔ سفارش قابل تغییر نیست." });
                TempData["CouponError"] = "کد کوپن مصرف‌شده برای حفظ سابقهٔ سفارش قابل تغییر نیست."; return RedirectToAction(nameof(Index));
            case CouponMutationResult.UsageLimitTooLow:
                if (IsAjaxRequest()) return BadRequest(new { success = false, message = "سقف استفاده نمی‌تواند کمتر از تعداد مصرف ثبت‌شده باشد." });
                TempData["CouponError"] = "سقف استفاده نمی‌تواند کمتر از تعداد مصرف ثبت‌شده باشد."; return RedirectToAction(nameof(Index));
        }
        if (IsAjaxRequest()) return Json(new { success = true, message = "کد تخفیف ذخیره شد." });
        TempData["CouponMessage"] = "کد تخفیف ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }
}

public sealed record CouponRow(
    decimal Id,
    string Code,
    int DiscountPercent,
    int UsageLimit,
    bool Enabled,
    DateOnly ExpiresOn,
    int Uses,
    decimal Discount,
    bool HasOrders,
    decimal? IdProfile,
    string? CustomerEmail);

public sealed record CouponIndexModel(
    List<CouponRow> Coupons,
    string Query,
    string Status,
    int Page,
    int Total,
    int Uses,
    decimal Discount,
    DateOnly Today);

public sealed record CouponOrderRow(
    decimal Id,
    string OrderNumber,
    string Customer,
    string Status,
    DateTime CreatedAtUtc,
    decimal Subtotal,
    decimal Discount,
    decimal Total);

public sealed record CouponReportModel(
    decimal Id,
    string Code,
    int UsageLimit,
    int Uses,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    int Page,
    List<CouponOrderRow> Orders);
