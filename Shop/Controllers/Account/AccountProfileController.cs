using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Application.Store;
using Shop.Services;
using Shop.Models;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;

namespace Shop.Controllers.Account;

[ApiController]
[Authorize(AuthenticationSchemes = "ShopCookie")]
[Route("api/account")]
public sealed class AccountProfileController : AccountApiControllerBase
{
    private readonly IPasswordHasher<string> passwordHasher;

    public AccountProfileController(ShopDbContext context, IPasswordHasher<string> hasher) : base(context)
    {
        passwordHasher = hasher;
    }
[HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(ProfileRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
            request.FirstName.Length > 100 || request.LastName.Length > 100 || (request.Phone?.Length ?? 0) > 30 ||
            request.IdGender is <= 0) return BadRequest(new { code = "invalid-profile" });

        if (request.IdGender.HasValue && !await db.AccountGenders
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.IdGender.Value, ct))
            return BadRequest(new { code = "invalid-profile" });

        var profile = await db.AccountProfiles
            .SingleOrDefaultAsync(x => x.Id == ProfileId, ct);
        if (profile is null) return Unauthorized();

        profile.FirstName = request.FirstName.Trim();
        profile.LastName = request.LastName.Trim();
        profile.Phone = request.Phone;
        profile.IdGender = request.IdGender;
        profile.NewsletterEnabled = request.Newsletter;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { saved = true });
    }

    [HttpPut("password")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("account-password")]
    public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.NewPassword) ||
            !string.Equals(request.NewPassword, request.NewPasswordConfirmation, StringComparison.Ordinal))
            return BadRequest(new
                { code = "invalid-new-password", message = "رمز جدید را وارد کنید و تکرار آن را مطابق بنویسید." });

        string email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var profile = await db.AccountProfiles
            .SingleOrDefaultAsync(x => x.Id == ProfileId, ct);
        if (string.IsNullOrWhiteSpace(email) || profile is null) return Unauthorized();

        string? currentPassword = profile.Password;
        if (currentPassword is not null &&
            (string.IsNullOrEmpty(request.CurrentPassword) ||
             (currentPassword.StartsWith("AQAAAA", StringComparison.Ordinal)
                 ? passwordHasher.VerifyHashedPassword(email, currentPassword, request.CurrentPassword) ==
                   PasswordVerificationResult.Failed
                 : currentPassword != request.CurrentPassword)))
            return BadRequest(new { code = "invalid-current-password", message = "رمز فعلی درست نیست." });

        if (currentPassword is null && !string.IsNullOrEmpty(request.CurrentPassword))
            return BadRequest(new { code = "invalid-current-password", message = "این حساب هنوز رمز عبور ندارد." });

        profile.Password = request.NewPassword;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { changed = true });
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress(AddressRequest a, CancellationToken ct)
    {
        if (new[]
            {
                a.Title, a.Recipient, a.Phone, a.Street, a.Postal
            }.Any(v => string.IsNullOrWhiteSpace(v)) ||
            a.Street!.Length > 500 || a.CountryId is null or < 1 || a.ProvinceId is null or < 1 ||
            a.CityId is null or < 1)
            return BadRequest(new { code = "invalid-address" });

        if (!await IsAddressGeographyValidAsync(a.CountryId.Value, a.ProvinceId.Value, a.CityId.Value, ct))
            return BadRequest(new { code = "invalid-address-location" });

        var address = new AccountAddress
        {
            IdProfile = ProfileId,
            Title = a.Title!.Trim(),
            Recipient = a.Recipient!.Trim(),
            Phone = a.Phone!.Trim(),
            IdCountry = a.CountryId.Value,
            IdProvince = a.ProvinceId.Value,
            IdCity = a.CityId.Value,
            Street = a.Street!.Trim(),
            PostalCode = a.Postal!.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AccountAddresses.Add(address);
        await db.SaveChangesAsync(ct);

        return Ok(new { saved = true, id = address.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) });
    }

    [HttpDelete("addresses/{id:long}")]
    public async Task<IActionResult> DeleteAddress(long id, CancellationToken ct)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var address = await db.AccountAddresses
            .SingleOrDefaultAsync(x => x.Id == id && x.IdProfile == ProfileId, ct);
        if (address is not null && await db.StoreCustomerOrders
                .AsNoTracking()
                .AnyAsync(x => x.IdAddress == id, ct))
            return Conflict(new { code = "address-has-orders" });
        if (address is not null) db.AccountAddresses.Remove(address);
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Ok(new { deleted = address is not null });
    }

    [HttpPut("addresses/{id:long}")]
    public async Task<IActionResult> UpdateAddress(long id, AddressRequest a, CancellationToken ct)
    {
        if (id < 1 || new[]
            {
                a.Title, a.Recipient, a.Phone, a.Street, a.Postal
            }.Any(v => string.IsNullOrWhiteSpace(v)) ||
            a.Street!.Length > 500 || a.CountryId is null or < 1 || a.ProvinceId is null or < 1 ||
            a.CityId is null or < 1)
            return BadRequest(new { code = "invalid-address" });

        if (!await IsAddressGeographyValidAsync(a.CountryId.Value, a.ProvinceId.Value, a.CityId.Value, ct))
            return BadRequest(new { code = "invalid-address-location" });

        var address = await db.AccountAddresses
            .SingleOrDefaultAsync(x => x.Id == id && x.IdProfile == ProfileId, ct);
        if (address is null) return NotFound(new { code = "address-not-found" });

        address.Title = a.Title!.Trim();
        address.Recipient = a.Recipient!.Trim();
        address.Phone = a.Phone!.Trim();
        address.IdCountry = a.CountryId.Value;
        address.IdProvince = a.ProvinceId.Value;
        address.IdCity = a.CityId.Value;
        address.Street = a.Street!.Trim();
        address.PostalCode = a.Postal!.Trim();
        await db.SaveChangesAsync(ct);

        return Ok(new { saved = true, id });
    }

    private async Task<bool> IsAddressGeographyValidAsync(long countryId, long provinceId, long cityId,
        CancellationToken cancellationToken) =>
        await db.GeoCities
            .AsNoTracking()
            .AnyAsync(city =>
                    city.Id == cityId && city.IdProvince == provinceId && city.IdCountry == countryId &&
                    db.GeoProvinces
                        .AsNoTracking()
                        .Any(province =>
                            province.Id == city.IdProvince && province.IdCountry == city.IdCountry),
                cancellationToken);
}
