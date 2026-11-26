using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Helpers;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/reports")]
public sealed class StoreReportsAdminApiController(IStoreReportsAdminData reports) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReport(
        [FromQuery] string type = "sales",
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] long? warehouseId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? detail = null,
        [FromQuery] long? id = null,
        [FromQuery] long? salesPlanReferenceId = null,
        CancellationToken cancellationToken = default)
    {
        bool fullAccess = User.IsInRole("system.full-access");
        long[] allowedWarehouseIds = AllowedWarehouseIds().ToArray();
        if (type == "warehouses")
            return Ok(await reports.ReadWarehousesAsync(fullAccess, allowedWarehouseIds, cancellationToken));

        if (!AdminListQuery.TryCreate(page, pageSize, null, out _))
            return BadRequest(new { title = "شماره یا اندازهٔ صفحه معتبر نیست." });

        if (!TryGetRange(from, to, out DateTime start, out DateTime end, out string? error))
            return BadRequest(new { title = error });

        if (type is "products" or "inventory" || detail == "inventory-movements")
            warehouseId = await reports.ReadDefaultSalesWarehouseIdAsync(fullAccess, allowedWarehouseIds,
                cancellationToken) ?? 0;
        if (!string.IsNullOrWhiteSpace(detail))
        {
            if (id is null or <= 0) return BadRequest(new { title = "شناسهٔ ردیف گزارش معتبر نیست." });
            var details = await reports.ReadDetailsAsync(detail, id.Value, start, end, warehouseId,
                salesPlanReferenceId, page, pageSize, fullAccess, allowedWarehouseIds, cancellationToken);
            return details is null ? BadRequest(new { title = "نوع جزئیات معتبر نیست." }) : Ok(details);
        }

        object? result = type switch
        {
            "sales" => await reports.ReadSalesAsync(start, end, page, pageSize, cancellationToken),
            "products" => await reports.ReadProductsAsync(start, end, warehouseId, page, pageSize,
                fullAccess, allowedWarehouseIds, cancellationToken),
            "inventory" => await reports.ReadInventoryAsync(start, end, warehouseId, page, pageSize,
                fullAccess, allowedWarehouseIds, cancellationToken),
            "customers" => await reports.ReadCustomersAsync(start, end, page, pageSize, cancellationToken),
            _ => null
        };

        return result is null ? BadRequest(new { title = "نوع گزارش معتبر نیست." }) : Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string type = "sales",
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] long? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetRange(from, to, out DateTime start, out DateTime end, out string? error))
            return BadRequest(new { title = error });

        bool fullAccess = User.IsInRole("system.full-access");
        long[] allowedWarehouseIds = AllowedWarehouseIds().ToArray();
        if (type is "products" or "inventory")
            warehouseId = await reports.ReadDefaultSalesWarehouseIdAsync(fullAccess, allowedWarehouseIds,
                cancellationToken) ?? 0;
        string[] keys = type switch
        {
            "sales" => ["date", "orderCount", "revenue", "discount"],
            "products" => ["productName", "quantity", "revenue", "orderCount", "stock"],
            "inventory" =>
            [
                "warehouseLabel", "productCode", "productName", "variantName", "planName", "quantity",
                "minimumQuantity", "lastMovement"
            ],
            "customers" => ["customerName", "email", "orderCount", "revenue", "firstPurchase"],
            _ => []
        };
        if (keys.Length == 0) return BadRequest(new { title = "نوع گزارش معتبر نیست." });

        string[] labels = keys.Select(ExportLabel).ToArray();

        return new AdminExcelFileResult($"store-{type}-report.xlsx", "گزارش", async (worksheet, ct) =>
        {
            await worksheet.WriteRowAsync(ct, labels);
            const int exportPageSize = AdminListQuery.ExportBatchSize;
            int page = 1;
            int total;
            do
            {
                ct.ThrowIfCancellationRequested();
                object report = type switch
                {
                    "sales" => await reports.ReadSalesAsync(start, end, page, exportPageSize, ct),
                    "products" => await reports.ReadProductsAsync(start, end, warehouseId, page, exportPageSize,
                        fullAccess, allowedWarehouseIds, ct),
                    "inventory" => await reports.ReadInventoryAsync(start, end, warehouseId, page, exportPageSize,
                        fullAccess, allowedWarehouseIds, ct),
                    _ => await reports.ReadCustomersAsync(start, end, page, exportPageSize, ct)
                };
                JsonElement payload = JsonSerializer.SerializeToElement(report);
                total = payload.GetProperty("total").GetInt32();
                foreach (JsonElement item in payload.GetProperty("items").EnumerateArray())
                    await worksheet.WriteRowAsync(ct, keys.Select(key => ExportValue(item, key)).ToArray());
                page++;
            } while ((long)(page - 1) * exportPageSize < total);
        });
    }

    private HashSet<long> AllowedWarehouseIds() => User.FindAll("admin_warehouse")
        .Select(claim => long.TryParse(claim.Value, out long id) ? id : 0).Where(id => id > 0).ToHashSet();

    private static bool TryGetRange(DateOnly? from, DateOnly? to, out DateTime start, out DateTime end,
        out string? error)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly endDate = to ?? today;
        DateOnly startDate = from ?? endDate.AddDays(-29);
        start = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        end = DateTime.SpecifyKind(endDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        if (startDate > endDate)
        {
            error = "تاریخ شروع باید قبل از تاریخ پایان باشد.";
            return false;
        }

        if ((endDate.DayNumber - startDate.DayNumber) > 365)
        {
            error = "بازهٔ گزارش نمی‌تواند بیشتر از ۳۶۶ روز باشد.";
            return false;
        }

        error = null;

        return true;
    }

    private static decimal ChangePercent(decimal current, decimal previous) =>
        previous == 0 ? current == 0 ? 0 : 100 : decimal.Round((current - previous) * 100 / previous, 1);

    private static object[] DetailColumns(params (string Key, string Label)[] columns) =>
        columns.Select(x => (object)new { key = x.Key, label = x.Label }).ToArray();

    private static string ExportLabel(string key) => key switch
    {
        "date" => "تاریخ",
        "orderCount" => "تعداد سفارش",
        "revenue" => "فروش / خرید (تومان)",
        "discount" => "تخفیف (تومان)",
        "productName" => "نام کالا",
        "quantity" => "تعداد",
        "stock" => "موجودی مجاز",
        "customerName" => "مشتری",
        "email" => "ایمیل",
        "firstPurchase" => "اولین خرید",
        "warehouseLabel" => "شرکت / انبار",
        "productCode" => "کد کالا",
        "variantName" => "تنوع",
        "planName" => "طرح",
        "minimumQuantity" => "حداقل موجودی",
        "lastMovement" => "آخرین گردش",
        _ => key
    };

    private static object? ExportValue(JsonElement item, string key)
    {
        if (!item.TryGetProperty(key, out JsonElement value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number when value.TryGetDecimal(out decimal number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => value.ToString()
        };
    }
}
