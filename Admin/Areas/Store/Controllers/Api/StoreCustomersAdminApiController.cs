using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Infrastructure;
using System.Net.Mail;
using System.Text.Json;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/customers")]
public sealed class StoreCustomersAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return ValidationProblem(detail: "عبارت جست‌وجو یا صفحه‌بندی معتبر نیست.", statusCode: 400);

        q = paging.Search;
        int total = await db.AccountProfiles.CountAsync(p => q == "" || (p.FirstName + " " + p.LastName).Contains(q)
                                                                     || p.Email.Contains(q) ||
                                                                     (p.Phone ?? "").Contains(q), cancellationToken);
        var rows = await db.AccountProfiles.AsNoTracking()
            .Where(p => q == "" || (p.FirstName + " " + p.LastName).Contains(q) || p.Email.Contains(q) ||
                        (p.Phone ?? "").Contains(q))
            .OrderByDescending(p => p.Id).Skip(paging.Offset).Take(paging.PageSize).Select(p => new CustomerRow
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone,
                CreatedAtUtc = p.CreatedAtUtc,
                OrderCount = db.StoreCustomerOrders.Count(o => o.IdProfile == p.Id)
            }).ToListAsync(cancellationToken);

        return Ok(AdminListQuery.CreateResult(rows, total, paging));
    }

    [HttpGet("{id:decimal}")]
    public async Task<IActionResult> Details(decimal id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();

        var customer = await db.AccountProfiles.AsNoTracking().Where(p => p.Id == id).Select(p => new CustomerDetails
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            Email = p.Email,
            Phone = p.Phone,
            CreatedAtUtc = p.CreatedAtUtc,
            OrderCount = db.StoreCustomerOrders.Count(o => o.IdProfile == p.Id),
            FavoriteCount = db.AccountFavorites.Count(f => f.IdProfile == p.Id),
            PriceAlertCount = db.AccountPriceAlerts.Count(a => a.IdProfile == p.Id),
            ArticleInteractionCount = db.AccountArticleInteractions.Count(a => a.IdProfile == p.Id)
        }).SingleOrDefaultAsync(cancellationToken);
        if (customer is not null)
        {
            var addresses = await db.AccountAddresses
                .AsNoTracking()
                .Where(address => address.IdProfile == id)
                .Join(
                    db.GeoCountries.AsNoTracking(),
                    address => address.IdCountry,
                    country => country.Id,
                    (address, country) => new { address, country })
                .Join(
                    db.GeoProvinces.AsNoTracking(),
                    addressCountry => addressCountry.address.IdProvince,
                    province => province.Id,
                    (addressCountry, province) => new { addressCountry.address, addressCountry.country, province })
                .Join(
                    db.GeoCities.AsNoTracking(),
                    addressCountryProvince => addressCountryProvince.address.IdCity,
                    city => city.Id,
                    (addressCountryProvince, city) => new
                    {
                        addressCountryProvince.address,
                        addressCountryProvince.country,
                        addressCountryProvince.province,
                        city
                    })
                .OrderBy(addressDetails => addressDetails.address.Id)
                .Select(addressDetails => new
                {
                    addressDetails.address.Title,
                    addressDetails.address.Recipient,
                    addressDetails.address.Phone,
                    Country = addressDetails.country.NameFa,
                    Province = addressDetails.province.NameFa,
                    City = addressDetails.city.NameFa,
                    addressDetails.address.Street,
                    addressDetails.address.PostalCode,
                    addressDetails.address.IsDefault
                })
                .ToListAsync(cancellationToken);
            var orders = await db.StoreCustomerOrders.AsNoTracking().Where(o => o.IdProfile == id)
                .OrderByDescending(o => o.Id).Take(50).Select(o => new
                {
                    o.OrderNumber,
                    Status = o.StatusReference.NameFa,
                    o.TotalAmount,
                    o.CreatedAtUtc
                }).ToListAsync(cancellationToken);
            var feedback = await db.ProductFeedback.AsNoTracking().Where(f => f.IdProfile == id)
                .OrderByDescending(f => f.Id)
                .Take(50).Select(f => new
                {
                    f.FeedbackType, f.FeedbackText, Status = f.Status.Name, f.CreatedAtUtc, ProductName = f.Product.NameFa
                })
                .ToListAsync(cancellationToken);
            var cartItems = await db.AccountCarts
                .AsNoTracking()
                .Join(
                    db.AccountCartItems.AsNoTracking(),
                    cart => cart.Id,
                    item => item.IdCart,
                    (cart, item) => new { cart, item })
                .Join(
                    db.Products.AsNoTracking(),
                    cartItem => cartItem.item.IdProduct,
                    product => product.Id,
                    (cartItem, product) => new { cartItem.cart, cartItem.item, product })
                .Join(
                    db.SalesPlanReferences.AsNoTracking(),
                    cartItemProduct => cartItemProduct.item.IdSalesPlan,
                    plan => plan.Id,
                    (cartItemProduct, plan) => new { cartItemProduct.cart, cartItemProduct.item,
                        cartItemProduct.product, plan })
                .Where(cartItemDetails => cartItemDetails.cart.IdProfile == id)
                .OrderBy(cartItemDetails => cartItemDetails.item.Id)
                .Select(cartItemDetails => new
                {
                    cartItemDetails.item.Id,
                    ProductName = cartItemDetails.product.NameFa,
                    IdProductVariant = cartItemDetails.item.IdVariant,
                    VariantPath = db.ProductVariants
                        .Where(variant => variant.Id == cartItemDetails.item.IdVariant)
                        .Select(variant => variant.VariantName.TreePathNames)
                        .FirstOrDefault(),
                    SalesPlanName = cartItemDetails.plan.DisplayName,
                    cartItemDetails.item.IdSalesPlan,
                    cartItemDetails.item.Quantity
                })
                .ToListAsync(cancellationToken);
            decimal[] cartItemIds = cartItems.Select(item => item.Id).ToArray();
            var cartAccessories = cartItemIds.Length == 0
                ? []
                : await db.AccountCartItemAccessories
                    .AsNoTracking()
                    .Join(
                        db.StoreProductAccessories.AsNoTracking(),
                        selected => selected.IdProductAccessory,
                        link => link.Id,
                        (selected, link) => new { selected, link })
                    .Join(
                        db.StoreAccessories.AsNoTracking(),
                        selectedLink => selectedLink.link.IdAccessory,
                        accessory => accessory.Id,
                        (selectedLink, accessory) => new { selectedLink.selected, selectedLink.link, accessory })
                    .Where(selectedAccessory => cartItemIds.Contains(selectedAccessory.selected.IdCartItem))
                    .OrderBy(selectedAccessory => selectedAccessory.selected.IdCartItem)
                    .ThenBy(selectedAccessory => selectedAccessory.link.SortOrder)
                    .ThenBy(selectedAccessory => selectedAccessory.link.Id)
                    .Select(selectedAccessory => new
                    {
                        selectedAccessory.selected.IdCartItem,
                        selectedAccessory.accessory.Name,
                        selectedAccessory.link.IsRequired
                    })
                    .ToListAsync(cancellationToken);
            customer.CartJson = JsonSerializer.Serialize(cartItems.Select(item => new
            {
                item.ProductName,
                item.VariantPath,
                item.IdSalesPlan,
                item.Quantity,
                Accessories = cartAccessories.Where(accessory => accessory.IdCartItem == item.Id)
                    .Select(accessory => new { accessory.Name, accessory.IsRequired })
            }));
            customer.AddressesJson = JsonSerializer.Serialize(addresses);
            customer.OrdersJson = JsonSerializer.Serialize(orders);
            customer.FeedbackJson = JsonSerializer.Serialize(feedback);
        }

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

        var profile = new Shop.Data.Domain.Account.Entities.AccountProfile
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AccountProfiles.Add(profile);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new { title = "این ایمیل از قبل برای مشتری دیگری ثبت شده است." });
        }

        return Ok(new { id = profile.Id, message = "مشتری ساخته شد؛ ورود او با کد یک‌بارمصرف ایمیل انجام می‌شود." });
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

        if (await db.AccountProfiles.AnyAsync(x => x.Id != id && x.Email == email, cancellationToken))
            return Conflict(new { title = "این ایمیل از قبل برای مشتری دیگری ثبت شده است." });

        try
        {
            int changed = await db.AccountProfiles.Where(x => x.Id == id && x.Email == input.ExpectedEmail &&
                                                              x.FirstName == input.ExpectedFirstName &&
                                                              x.LastName == input.ExpectedLastName &&
                                                              x.Phone == input.ExpectedPhone)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Email, email)
                        .SetProperty(x => x.FirstName, firstName).SetProperty(x => x.LastName, lastName)
                        .SetProperty(x => x.Phone, phone).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow),
                    cancellationToken);

            return changed == 0
                ? Conflict(new { title = "اطلاعات مشتری تغییر کرده یا مشتری پیدا نشد؛ فهرست را دوباره بارگذاری کنید." })
                : Ok(new { message = "اطلاعات مشتری ذخیره شد." });
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return Conflict(new { title = "این ایمیل از قبل برای مشتری دیگری ثبت شده است." });
        }
    }

    [HttpDelete("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();

        if (!await db.AccountProfiles.AnyAsync(x => x.Id == id, cancellationToken)) return NotFound();

        bool hasOrders = await db.Set<Shop.Data.Domain.Account.Entities.StoreCustomerOrderReference>()
            .AnyAsync(x => x.IdProfile == id, cancellationToken);
        bool hasCart = await db.AccountCarts.AnyAsync(x => x.IdProfile == id, cancellationToken);
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
