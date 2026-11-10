using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/customers")]
public sealed class StoreCustomersAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        q = q?.Trim() ?? "";
        if (q.Length > 100 || page is < 1 or > 100000 || pageSize is < 1 or > 100)
            return BadRequest(new { title = "جست‌وجو یا صفحه‌بندی نامعتبر است." });
        int offset = (page - 1) * pageSize, take = pageSize + 1;
        var rows = await db.Database.SqlQuery<CustomerRow>($"""
            SELECT p.[Id], p.[FirstName], p.[LastName], p.[Email], p.[Phone], p.[CreatedAtUtc],
                (SELECT COUNT(*) FROM [Store].[Order] o WHERE o.[IdProfile] = p.[Id]) AS [OrderCount]
            FROM [Account].[Profile] p
            WHERE {q} = N'' OR CHARINDEX({q}, CONCAT(p.[FirstName], N' ', p.[LastName])) > 0
                OR CHARINDEX({q}, p.[Email]) > 0 OR CHARINDEX({q}, COALESCE(p.[Phone], N'')) > 0
            ORDER BY p.[Id] DESC OFFSET {offset} ROWS FETCH NEXT {take} ROWS ONLY
            """).ToListAsync(cancellationToken);
        return Ok(new { items = rows.Take(pageSize), page, hasNext = rows.Count > pageSize });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        var customer = await db.Database.SqlQuery<CustomerDetails>($"""
            SELECT p.[Id], p.[FirstName], p.[LastName], p.[Email], p.[Phone], p.[CreatedAtUtc],
                (SELECT COUNT(*) FROM [Store].[Order] o WHERE o.[IdProfile] = p.[Id]) AS [OrderCount],
                COALESCE((SELECT a.[Title], a.[Recipient], a.[Phone], country.[NameFa] AS [Country], province.[NameFa] AS [Province], city.[NameFa] AS [City], a.[Street], a.[PostalCode], a.[IsDefault]
                    FROM [Account].[Address] a JOIN [Geo].[Country] country ON country.[Id] = a.[IdCountry]
                    JOIN [Geo].[Province] province ON province.[Id] = a.[IdProvince] AND province.[IdCountry] = a.[IdCountry]
                    JOIN [Geo].[City] city ON city.[Id] = a.[IdCity] AND city.[IdProvince] = a.[IdProvince] AND city.[IdCountry] = a.[IdCountry]
                    WHERE a.[IdProfile] = p.[Id] ORDER BY a.[Id] FOR JSON PATH), N'[]') AS [AddressesJson],
                COALESCE((SELECT TOP (50) o.[OrderNumber], o.[Status], o.[TotalAmount], o.[OrderedAtUtc],
                    CONVERT(bit, CASE WHEN o.[PaymentMethod] = N'demo' AND o.[ExternalOrderKey] LIKE N'demo-order-%' THEN 1 ELSE 0 END) AS [IsSample]
                    FROM [Store].[Order] o WHERE o.[IdProfile] = p.[Id] ORDER BY o.[Id] DESC FOR JSON PATH), N'[]') AS [OrdersJson],
                COALESCE((SELECT TOP (50) f.[FeedbackType], f.[FeedbackText], f.[Status], f.[CreatedAtUtc], f.[IsSample], product.[Name] AS [ProductName]
                    FROM [Store].[ProductFeedback] f JOIN [Warehouse].[Product] product ON product.[Id] = f.[IdProduct]
                    WHERE f.[IdProfile] = p.[Id] ORDER BY f.[Id] DESC FOR JSON PATH), N'[]') AS [FeedbackJson],
                (SELECT COUNT(*) FROM [Account].[Favorite] f WHERE f.[IdProfile] = p.[Id]) AS [FavoriteCount],
                (SELECT COUNT(*) FROM [Account].[PriceAlert] a WHERE a.[IdProfile] = p.[Id]) AS [PriceAlertCount],
                (SELECT COUNT(*) FROM [Account].[ArticleInteraction] a WHERE a.[IdProfile] = p.[Id]) AS [ArticleInteractionCount]
            FROM [Account].[Profile] p WHERE p.[Id] = {id}
            """).SingleOrDefaultAsync(cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
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
        if (await db.AccountProfiles.AnyAsync(x => x.Email == email, cancellationToken))
            return Conflict(new { title = "این ایمیل از قبل برای مشتری دیگری ثبت شده است." });
        var profile = new Shop.Database.Domain.Account.Entities.AccountProfile
        {
            Email = email, FirstName = firstName, LastName = lastName, Phone = phone,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AccountProfiles.Add(profile);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        { return Conflict(new { title = "این ایمیل از قبل برای مشتری دیگری ثبت شده است." }); }
        return Ok(new { id = profile.Id, message = "مشتری ساخته شد؛ ورود او با کد یک‌بارمصرف ایمیل انجام می‌شود." });
    }

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(long id, [FromBody] CustomerUpdate input, CancellationToken cancellationToken)
    {
        if (!TryNormalizeEmail(input.Email, out var email) || input.ExpectedEmail is null || input.ExpectedEmail.Length > 254)
            return BadRequest(new { title = "ایمیل معتبر نیست یا نسخهٔ مبنا ارسال نشده است." });
        string firstName = input.FirstName?.Trim() ?? "", lastName = input.LastName?.Trim() ?? "";
        string? phone = NormalizePhone(input.Phone);
        if (id <= 0 || !ValidName(firstName) || !ValidName(lastName) || (phone?.Length ?? 0) > 30 ||
            input.ExpectedFirstName is null || input.ExpectedLastName is null || input.ExpectedFirstName.Length > 100 ||
            input.ExpectedLastName.Length > 100 || (input.ExpectedPhone?.Length ?? 0) > 30)
            return BadRequest(new { title = "نام، نام خانوادگی یا تلفن معتبر نیست." });
        if (await db.AccountProfiles.AnyAsync(x => x.Id != id && x.Email == email, cancellationToken))
            return Conflict(new { title = "این ایمیل از قبل برای مشتری دیگری ثبت شده است." });
        try
        {
            int changed = await db.AccountProfiles.Where(x => x.Id == id && x.Email == input.ExpectedEmail &&
                    x.FirstName == input.ExpectedFirstName && x.LastName == input.ExpectedLastName && x.Phone == input.ExpectedPhone)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Email, email)
                    .SetProperty(x => x.FirstName, firstName).SetProperty(x => x.LastName, lastName)
                    .SetProperty(x => x.Phone, phone).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
            return changed == 0 ? Conflict(new { title = "اطلاعات مشتری تغییر کرده یا مشتری پیدا نشد؛ فهرست را دوباره بارگذاری کنید." })
                : Ok(new { message = "اطلاعات مشتری ذخیره شد." });
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        { return Conflict(new { title = "این ایمیل از قبل برای مشتری دیگری ثبت شده است." }); }
    }

    [HttpDelete("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        if (!await db.AccountProfiles.AnyAsync(x => x.Id == id, cancellationToken)) return NotFound();
        bool hasOrders = await db.Set<Shop.Database.Domain.Account.Entities.AccountCustomerOrderReference>().AnyAsync(x => x.IdProfile == id, cancellationToken)
            || await db.Set<Shop.Database.Domain.Account.Entities.StoreCustomerOrderReference>().AnyAsync(x => x.IdProfile == id, cancellationToken);
        bool hasCart = await db.Set<Shop.Database.Domain.Account.Entities.AccountCartReference>().AnyAsync(x => x.IdProfile == id, cancellationToken);
        if (hasOrders || hasCart)
            return Conflict(new { title = "مشتری دارای سفارش یا سبد خرید است؛ برای حفظ سابقه، حذف ممکن نیست." });
        await db.AccountProfiles.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        return Ok(new { message = "پروفایل مشتری حذف شد؛ داده‌های وابستهٔ حساب طبق روابط پایگاه پاک شدند." });
    }

    private static bool ValidName(string value) => value.Length is >= 1 and <= 100;
    private static string? NormalizePhone(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool TryNormalizeEmail(string? value, out string email)
    {
        email = (value ?? "").Trim().ToLowerInvariant();
        try { return email.Length <= 254 && new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase); }
        catch (FormatException) { return false; }
    }

    public sealed record CustomerCreate(string? Email, string? FirstName, string? LastName, string? Phone);
    public sealed record CustomerUpdate(string? Email, string? FirstName, string? LastName, string? Phone,
        string? ExpectedEmail, string? ExpectedFirstName, string? ExpectedLastName, string? ExpectedPhone);

    public class CustomerRow
    {
        public long Id { get; set; }
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
        public int FavoriteCount { get; set; }
        public int PriceAlertCount { get; set; }
        public int ArticleInteractionCount { get; set; }
    }
}
