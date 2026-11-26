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
public sealed class AccountListsController : AccountApiControllerBase
{
    private readonly IAccountDataCommands data;
    public AccountListsController(IAccountDataCommands accountData) : base(accountData) => data = accountData;

    [HttpPost("lists/{kind}/{productId:long}")]
    public async Task<IActionResult> ToggleList(string kind, long productId, CancellationToken ct)
    {
        if (kind is not ("favorites" or "compare") || productId < 1)
            return BadRequest(new { code = "invalid-list-item" });

        bool? changed = await data.ToggleListItemAsync(ProfileId, kind, productId, ct);
        if (changed is null) return Conflict(new { code = "product-unavailable" });

        return Ok(new { changed = changed.Value });
    }

    [HttpPut("lists/{kind}")]
    public async Task<IActionResult> SaveList(string kind, [FromBody] ListRequest request, CancellationToken ct)
    {
        if (kind is not ("favorites" or "compare") || request.ProductIds is null || request.ProductIds.Count > 100 ||
            request.ProductIds.Any(id => id < 1) ||
            request.ProductIds.Distinct().Count() != request.ProductIds.Count)
            return BadRequest(new { code = "invalid-list" });

        decimal[] productIds = request.ProductIds.ToArray();
        if (!await data.ReplaceListAsync(ProfileId, kind, productIds, ct))
            return Conflict(new { code = "product-unavailable" });

        return Ok(new { saved = true });
    }

    [HttpPut("price-alerts")]
    public async Task<IActionResult> SavePriceAlerts([FromBody] PriceAlertListRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count > 100)
            return BadRequest(new { code = "invalid-price-alerts" });

        var normalized = new List<AccountPriceAlertData>();
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

            normalized.Add(new AccountPriceAlertData(productId, item.Scope!, signature, variants,
                item.PriceAtRegistration, item.StockAtRegistration,
                DateTime.SpecifyKind(item.CreatedAt.UtcDateTime, DateTimeKind.Utc)));
        }

        if (!await data.ReplacePriceAlertsAsync(ProfileId, normalized, ct))
            return Conflict(new { code = "product-unavailable" });

        return Ok(new { saved = true });
    }
}
