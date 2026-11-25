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
public sealed class AccountListsController : AccountApiControllerBase
{
    public AccountListsController(ShopDbContext context) : base(context) { }

    [HttpPost("lists/{kind}/{productId:long}")]
    public async Task<IActionResult> ToggleList(string kind, long productId, CancellationToken ct)
    {
        if (kind is not ("favorites" or "compare") || productId < 1)
            return BadRequest(new { code = "invalid-list-item" });

        if (!await db.Products
                .AsNoTracking()
                .AnyAsync(x => x.Id == productId && x.IsStoreEnabled, ct))
            return Conflict(new { code = "product-unavailable" });

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        bool changed;
        if (kind == "favorites")
        {
            var existing =
                await db.AccountFavorites
                    .SingleOrDefaultAsync(
                    x => x.IdProfile == ProfileId && x.IdProduct == productId, ct);
            changed = existing is null;
            if (existing is null)
                db.AccountFavorites.Add(new AccountFavorite
                    { IdProfile = ProfileId, IdProduct = productId, CreatedAtUtc = DateTime.UtcNow });
            else db.AccountFavorites.Remove(existing);
        }
        else
        {
            var existing =
                await db.AccountCompareItems
                    .SingleOrDefaultAsync(
                    x => x.IdProfile == ProfileId && x.IdProduct == productId, ct);
            changed = existing is null;
            if (existing is null)
                db.AccountCompareItems.Add(new AccountCompareItem
                    { IdProfile = ProfileId, IdProduct = productId, CreatedAtUtc = DateTime.UtcNow });
            else db.AccountCompareItems.Remove(existing);
        }

        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Ok(new { changed });
    }

    [HttpPut("lists/{kind}")]
    public async Task<IActionResult> SaveList(string kind, [FromBody] ListRequest request, CancellationToken ct)
    {
        if (kind is not ("favorites" or "compare") || request.ProductIds is null || request.ProductIds.Count > 100 ||
            request.ProductIds.Any(id => id < 1) ||
            request.ProductIds.Distinct().Count() != request.ProductIds.Count)
            return BadRequest(new { code = "invalid-list" });

        decimal[] productIds = request.ProductIds.ToArray();
        int availableProductCount = await db.Products
            .AsNoTracking()
            .CountAsync(x => productIds.Contains(x.Id) && x.IsStoreEnabled, ct);
        if (availableProductCount != productIds.Length)
            return Conflict(new { code = "product-unavailable" });

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (kind == "favorites")
        {
            await db.AccountFavorites
                .Where(x => x.IdProfile == ProfileId)
                .ExecuteDeleteAsync(ct);
            db.AccountFavorites.AddRange(productIds.Select(id => new AccountFavorite
                { IdProfile = ProfileId, IdProduct = id, CreatedAtUtc = DateTime.UtcNow }));
        }
        else
        {
            await db.AccountCompareItems
                .Where(x => x.IdProfile == ProfileId)
                .ExecuteDeleteAsync(ct);
            db.AccountCompareItems.AddRange(productIds.Select(id => new AccountCompareItem
                { IdProfile = ProfileId, IdProduct = id, CreatedAtUtc = DateTime.UtcNow }));
        }

        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Ok(new { saved = true });
    }

    [HttpPut("price-alerts")]
    public async Task<IActionResult> SavePriceAlerts([FromBody] PriceAlertListRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count > 100)
            return BadRequest(new { code = "invalid-price-alerts" });

        var normalized = new List<object>();
        var configurations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in request.Items)
        {
            if (item is null || !long.TryParse(item.ProductId, out long productId) || productId < 1 ||
                item.Scope is not ("all" or "variant") || item.PriceAtRegistration is < 0 or > 9999999999999999999m ||
                decimal.Truncate(item.PriceAtRegistration) != item.PriceAtRegistration ||
                (item.StockAtRegistration?.Length ?? 0) > 100 ||
                item.Scope == "variant" && item.Variants is not { ValueKind: System.Text.Json.JsonValueKind.Object })
                return BadRequest(new { code = "invalid-price-alert" });

            string? variants = item.Variants is { ValueKind: System.Text.Json.JsonValueKind.Object } value
                ? value.GetRawText()
                : null;
            string signature = variants ?? "";
            if (signature.Length > 500) return BadRequest(new { code = "invalid-price-alert" });

            if (!configurations.Add($"{productId}:{item.Scope}:{signature}"))
                return BadRequest(new { code = "duplicate-price-alert" });

            normalized.Add(new
            {
                productId,
                scope = item.Scope,
                signature,
                variantsJson = variants is null ? (System.Text.Json.JsonElement?)null : item.Variants,
                item.PriceAtRegistration,
                item.StockAtRegistration,
                item.CreatedAt
            });
        }

        decimal[] alertProductIds = normalized
            .Select(item => (decimal)item.GetType().GetProperty("productId")!.GetValue(item)!).Distinct().ToArray();
        int availableAlertProducts = await db.Products
            .AsNoTracking()
            .CountAsync(x => alertProductIds.Contains(x.Id) && x.IsStoreEnabled, ct);
        if (availableAlertProducts != alertProductIds.Length)
            return Conflict(new { code = "product-unavailable" });

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await db.AccountPriceAlerts
            .Where(x => x.IdProfile == ProfileId)
            .ExecuteDeleteAsync(ct);
        foreach (dynamic item in normalized)
        {
            long productId = item.productId;
            string scope = item.scope;
            string signature = item.signature;
            string? variantsJson = item.variantsJson?.GetRawText();
            DateTime createdAt = item.CreatedAt is DateTime date
                ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                : DateTime.UtcNow;
            db.AccountPriceAlerts.Add(new AccountPriceAlert
            {
                IdProfile = ProfileId,
                IdProduct = productId,
                Scope = scope,
                VariantSignature = signature,
                VariantsJson = variantsJson,
                PriceAtRegistration = item.PriceAtRegistration,
                StockAtRegistration = item.StockAtRegistration,
                CreatedAtUtc = createdAt
            });
        }

        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Ok(new { saved = true });
    }
}
