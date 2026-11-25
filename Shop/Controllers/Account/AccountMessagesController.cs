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
public sealed class AccountMessagesController : AccountApiControllerBase
{
    public AccountMessagesController(ShopDbContext context) : base(context) { }

    [HttpGet("messages")]
    public async Task<IActionResult> Messages(CancellationToken ct)
    {
        var items = await db.AccountMessages
            .AsNoTracking()
            .Where(x => x.IdProfile == ProfileId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Category,
                x.Body,
                unread = !x.IsRead,
                x.CreatedAtUtc
            })
            .ToListAsync(ct);

        return Ok(items.Select(item => new
        {
            item.Id, item.Title, item.Category, Body = StoreHtmlSanitizer.Sanitize(item.Body), item.unread,
            item.CreatedAtUtc
        }));
    }

    [HttpPut("messages/{id:long}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkMessageRead(long id, CancellationToken ct)
    {
        var item = await db.AccountMessages
            .SingleOrDefaultAsync(x => x.Id == id && x.IdProfile == ProfileId, ct);
        if (item is null) return NotFound();

        if (!item.IsRead)
        {
            item.IsRead = true;
            item.ReadAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Ok(new { message = "پیام خوانده‌شده علامت خورد." });
    }

    [HttpGet("recent-searches")]
    public async Task<IActionResult> RecentSearches(CancellationToken ct)
    {
        var items = await db.AccountRecentSearches
            .AsNoTracking()
            .Where(x => x.IdProfile == ProfileId)
            .OrderByDescending(x => x.SearchedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => x.SearchText)
            .Take(6)
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpPost("recent-searches")]
    public async Task<IActionResult> AddRecentSearch([FromBody] RecentSearchRequest request, CancellationToken ct)
    {
        string searchText = (request.Query ?? string.Empty).Trim();
        if (searchText.Length is < 3 or > 80) return BadRequest(new { code = "invalid-search-query" });

        string searchKey = searchText.ToLowerInvariant();
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var existing = await db.AccountRecentSearches
            .SingleOrDefaultAsync(
                x => x.IdProfile == ProfileId && x.SearchKey == searchKey, ct);
        if (existing is null)
        {
            db.AccountRecentSearches.Add(new AccountRecentSearch
            {
                IdProfile = ProfileId, SearchKey = searchKey, SearchText = searchText, SearchedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.SearchText = searchText;
            existing.SearchedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        var keep = await db.AccountRecentSearches
            .AsNoTracking()
            .Where(x => x.IdProfile == ProfileId)
            .OrderByDescending(x => x.SearchedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => x.Id)
            .Take(6)
            .ToListAsync(ct);
        await db.AccountRecentSearches
            .Where(x => x.IdProfile == ProfileId && !keep.Contains(x.Id))
            .ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);

        return Ok(new { saved = true });
    }

    [HttpDelete("recent-searches")]
    public async Task<IActionResult> ClearRecentSearches(CancellationToken ct)
    {
        await db.AccountRecentSearches
            .Where(x => x.IdProfile == ProfileId)
            .ExecuteDeleteAsync(ct);

        return Ok(new { deleted = true });
    }
}
