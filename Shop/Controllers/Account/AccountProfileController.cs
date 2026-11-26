using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Account.Contracts;
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

    private readonly IAccountDataCommands data;

    public AccountProfileController(IPasswordHasher<string> hasher, IAccountDataCommands accountData) :
        base(accountData)
    {
        passwordHasher = hasher;
        data = accountData;
    }
[HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(ProfileRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
            request.FirstName.Length > 100 || request.LastName.Length > 100 || (request.Phone?.Length ?? 0) > 30 ||
            request.IdGender is <= 0) return BadRequest(new { code = "invalid-profile" });

        if (request.IdGender.HasValue && !await data.GenderExistsAsync(request.IdGender.Value, ct))
            return BadRequest(new { code = "invalid-profile" });
        if (!await data.UpdateProfileAsync(ProfileId, new AccountProfileUpdate(request.FirstName.Trim(),
                request.LastName.Trim(), request.Phone, request.IdGender, request.Newsletter), ct))
            return Unauthorized();

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
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();
        string? currentPassword = await data.ReadPasswordAsync(ProfileId, ct);
        if (currentPassword is not null &&
            (string.IsNullOrEmpty(request.CurrentPassword) ||
             (currentPassword.StartsWith("AQAAAA", StringComparison.Ordinal)
                 ? passwordHasher.VerifyHashedPassword(email, currentPassword, request.CurrentPassword) ==
                   PasswordVerificationResult.Failed
                 : currentPassword != request.CurrentPassword)))
            return BadRequest(new { code = "invalid-current-password", message = "رمز فعلی درست نیست." });

        if (currentPassword is null && !string.IsNullOrEmpty(request.CurrentPassword))
            return BadRequest(new { code = "invalid-current-password", message = "این حساب هنوز رمز عبور ندارد." });

        if (!await data.SetPasswordAsync(ProfileId, request.NewPassword, ct)) return Unauthorized();

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

        if (!await data.IsAddressLocationValidAsync(a.CountryId.Value, a.ProvinceId.Value, a.CityId.Value, ct))
            return BadRequest(new { code = "invalid-address-location" });
        decimal id = await data.AddAddressAsync(ProfileId, MapAddress(a), ct);
        return Ok(new { saved = true, id = id.ToString(System.Globalization.CultureInfo.InvariantCulture) });
    }

    [HttpDelete("addresses/{id:long}")]
    public async Task<IActionResult> DeleteAddress(long id, CancellationToken ct)
    {
        var result = await data.DeleteAddressAsync(ProfileId, id, ct);
        if (result == AccountAddressDeleteResult.HasOrders)
            return Conflict(new { code = "address-has-orders" });
        return Ok(new { deleted = result == AccountAddressDeleteResult.Deleted });
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

        if (!await data.IsAddressLocationValidAsync(a.CountryId.Value, a.ProvinceId.Value, a.CityId.Value, ct))
            return BadRequest(new { code = "invalid-address-location" });
        if (!await data.UpdateAddressAsync(ProfileId, id, MapAddress(a), ct))
            return NotFound(new { code = "address-not-found" });

        return Ok(new { saved = true, id });
    }

    private static AccountAddressUpdate MapAddress(AddressRequest request) => new(request.Title!.Trim(),
        request.Recipient!.Trim(), request.Phone!.Trim(), request.CountryId!.Value, request.ProvinceId!.Value,
        request.CityId!.Value, request.Street!.Trim(), request.Postal!.Trim());
}
