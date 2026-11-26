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
public sealed class AccountMessagesController : AccountApiControllerBase
{
    private readonly IAccountDataCommands data;
    public AccountMessagesController(IAccountDataCommands accountData) : base(accountData) => data = accountData;

    [HttpGet("messages")]
    public async Task<IActionResult> Messages(CancellationToken ct)
    {
        var items = await data.ReadMessagesAsync(ProfileId, ct);

        return Ok(items.Select(item => new
        {
            item.Id, item.Title, item.Category, Body = StoreHtmlSanitizer.Sanitize(item.Body), item.Unread,
            item.CreatedAtUtc
        }));
    }

    [HttpPut("messages/{id:long}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkMessageRead(long id, CancellationToken ct)
    {
        if (!await data.MarkMessageReadAsync(ProfileId, id, ct)) return NotFound();

        return Ok(new { message = "پیام خوانده‌شده علامت خورد." });
    }

    [HttpGet("recent-searches")]
    public async Task<IActionResult> RecentSearches(CancellationToken ct)
    {
        var items = await data.ReadRecentSearchesAsync(ProfileId, ct);

        return Ok(new { items });
    }

    [HttpPost("recent-searches")]
    public async Task<IActionResult> AddRecentSearch([FromBody] RecentSearchRequest request, CancellationToken ct)
    {
        string searchText = (request.Query ?? string.Empty).Trim();
        if (searchText.Length is < 3 or > 80) return BadRequest(new { code = "invalid-search-query" });

        string searchKey = searchText.ToLowerInvariant();
        await data.SaveRecentSearchAsync(ProfileId, searchText, searchKey, ct);

        return Ok(new { saved = true });
    }

    [HttpDelete("recent-searches")]
    public async Task<IActionResult> ClearRecentSearches(CancellationToken ct)
    {
        await data.ClearRecentSearchesAsync(ProfileId, ct);

        return Ok(new { deleted = true });
    }
}
