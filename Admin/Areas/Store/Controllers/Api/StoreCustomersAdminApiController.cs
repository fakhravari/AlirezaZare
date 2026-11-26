using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using System.Net.Mail;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/customers")]
public sealed class StoreCustomersAdminApiController(IStoreCustomersAdminData data) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return ValidationProblem(detail: "عبارت جست‌وجو یا صفحه‌بندی معتبر نیست.", statusCode: 400);

        var result = await data.ReadAsync(paging.Search, paging.Offset, paging.PageSize, cancellationToken);
        return Ok(AdminListQuery.CreateResult(result.Items.Select(ToRow).ToList(), result.Total, paging));
    }

    [HttpGet("{id:decimal}")]
    public async Task<IActionResult> Details(decimal id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();

        var result = await data.ReadDetailsAsync(id, cancellationToken);
        if (result is null) return NotFound();
        var customer = new CustomerDetails
        {
            Id = result.Customer.Id, FirstName = result.Customer.FirstName, LastName = result.Customer.LastName,
            Email = result.Customer.Email, Phone = result.Customer.Phone, CreatedAtUtc = result.Customer.CreatedAtUtc,
            OrderCount = result.Customer.OrderCount, AddressesJson = result.AddressesJson, OrdersJson = result.OrdersJson,
            FeedbackJson = result.FeedbackJson, CartJson = result.CartJson, FavoriteCount = result.FavoriteCount,
            PriceAlertCount = result.PriceAlertCount, ArticleInteractionCount = result.ArticleInteractionCount
        };
        return Ok(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CustomerCreate input, CancellationToken cancellationToken)
    {
        if (!TryNormalizeEmail(input.Email, out var email)) return BadRequest(new { title = "ایمیل معتبر وارد کنید." });

        string firstName = input.FirstName?.Trim() ?? "", lastName = input.LastName?.Trim() ?? "";
        string? phone = NormalizePhone(input.Phone);
        if (!ValidName(firstName) || !ValidName(lastName) || (phone?.Length ?? 0) > 30)
            return BadRequest(new { title = "نام، نام خانوادگی یا تلفن معتبر نیست." });

        var result = await data.CreateAsync(email, firstName, lastName, phone, cancellationToken);
        return result.Result == CustomerMutationResult.EmailExists
            ? Conflict(new { title = "این ایمیل از قبل برای مشتری دیگری ثبت شده است." })
            : Ok(new { id = result.Id, message = "مشتری ساخته شد؛ ورود او با کد یک‌بارمصرف ایمیل انجام می‌شود." });
    }

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(long id, [FromBody] CustomerUpdate input,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeEmail(input.Email, out var email) || input.ExpectedEmail is null ||
            input.ExpectedEmail.Length > 254)
            return BadRequest(new { title = "ایمیل معتبر نیست یا نسخهٔ مبنا ارسال نشده است." });

        string firstName = input.FirstName?.Trim() ?? "", lastName = input.LastName?.Trim() ?? "";
        string? phone = NormalizePhone(input.Phone);
        if (id <= 0 || !ValidName(firstName) || !ValidName(lastName) || (phone?.Length ?? 0) > 30 ||
            input.ExpectedFirstName is null || input.ExpectedLastName is null || input.ExpectedFirstName.Length > 100 ||
            input.ExpectedLastName.Length > 100 || (input.ExpectedPhone?.Length ?? 0) > 30)
            return BadRequest(new { title = "نام، نام خانوادگی یا تلفن معتبر نیست." });

        var result = await data.UpdateAsync(id, new CustomerAdminUpdate(email, firstName, lastName, phone,
            input.ExpectedEmail, input.ExpectedFirstName, input.ExpectedLastName, input.ExpectedPhone),
            cancellationToken);
        return result switch
        {
            CustomerMutationResult.EmailExists => Conflict(new { title = "این ایمیل از قبل برای مشتری دیگری ثبت شده است." }),
            CustomerMutationResult.Changed => Conflict(new { title = "اطلاعات مشتری تغییر کرده یا مشتری پیدا نشد؛ فهرست را دوباره بارگذاری کنید." }),
            _ => Ok(new { message = "اطلاعات مشتری ذخیره شد." })
        };
    }

    [HttpDelete("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();

        var result = await data.DeleteAsync(id, cancellationToken);
        return result switch
        {
            CustomerMutationResult.NotFound => NotFound(),
            CustomerMutationResult.HasOrdersOrCart => Conflict(new { title = "مشتری دارای سفارش یا سبد خرید است؛ برای حفظ سابقه، حذف ممکن نیست." }),
            _ => Ok(new { message = "پروفایل مشتری حذف شد؛ داده‌های وابستهٔ حساب طبق روابط پایگاه پاک شدند." })
        };
    }

    private static CustomerRow ToRow(CustomerAdminRow x) => new()
    {
        Id = x.Id, FirstName = x.FirstName, LastName = x.LastName, Email = x.Email, Phone = x.Phone,
        CreatedAtUtc = x.CreatedAtUtc, OrderCount = x.OrderCount
    };

    private static bool ValidName(string value) => value.Length is >= 1 and <= 100;
    private static string? NormalizePhone(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryNormalizeEmail(string? value, out string email)
    {
        email = (value ?? "").Trim().ToLowerInvariant();
        try
        {
            return email.Length <= 254 &&
                   new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public sealed record CustomerCreate(string? Email, string? FirstName, string? LastName, string? Phone);

    public sealed record CustomerUpdate(
        string? Email,
        string? FirstName,
        string? LastName,
        string? Phone,
        string? ExpectedEmail,
        string? ExpectedFirstName,
        string? ExpectedLastName,
        string? ExpectedPhone);

    public class CustomerRow
    {
        public decimal Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int OrderCount { get; set; }
    }

    public sealed class CustomerDetails : CustomerRow
    {
        public string AddressesJson { get; set; } = "[]";
        public string OrdersJson { get; set; } = "[]";
        public string FeedbackJson { get; set; } = "[]";
        public string CartJson { get; set; } = "[]";
        public int FavoriteCount { get; set; }
        public int PriceAlertCount { get; set; }
        public int ArticleInteractionCount { get; set; }
    }
}
