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
public sealed class AccountArticlesController : AccountApiControllerBase
{
    public AccountArticlesController(ShopDbContext context) : base(context) { }

    [HttpPost("articles/{slug}/interactions/{type}")]
    public async Task<IActionResult> ToggleArticleInteraction(string slug, string type, CancellationToken ct)
    {
        if (slug.Length is < 1 or > 160 || !slug.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ||
            type is not ("like" or "bookmark" or "read")) return BadRequest(new { code = "invalid-interaction" });

        var articleId = await db.MagazineArticles
            .AsNoTracking()
            .Where(x => x.ArticleKey == slug && x.IsPublished)
            .Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(ct);
        if (articleId is null) return NotFound(new { code = "article-not-found" });

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var interaction = await db.AccountArticleInteractions
            .SingleOrDefaultAsync(x =>
            x.IdProfile == ProfileId && x.IdMagazineArticle == articleId.Value && x.InteractionType == type, ct);
        if (interaction is null)
            db.AccountArticleInteractions.Add(new AccountArticleInteraction
                { IdProfile = ProfileId, IdMagazineArticle = articleId.Value, InteractionType = type });
        else
            db.AccountArticleInteractions.Remove(interaction);
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Ok(new { saved = true });
    }

    [HttpPost("articles/{slug}/feedback")]
    public async Task<IActionResult> AddArticleFeedback(string slug, [FromBody] ArticleFeedbackRequest request,
        CancellationToken ct)
    {
        string name = (request.Name ?? "").Trim();
        string text = (request.Text ?? "").Trim();
        if (slug.Length is < 1 or > 160 || !slug.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ||
            name.Length is < 3 or > 80 || name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2 ||
            text.Length is < 1 or > 1000 ||
            request.Rating is < 1 or > 5) return BadRequest(new { code = "invalid-feedback" });

        var articleId = await db.MagazineArticles
            .AsNoTracking()
            .Where(x => x.ArticleKey == slug && x.IsPublished)
            .Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(ct);
        if (articleId is null) return NotFound(new { code = "article-not-found" });

        var feedback = new AccountArticleFeedback
        {
            IdProfile = ProfileId,
            IdMagazineArticle = articleId.Value,
            DisplayName = name,
            FeedbackText = text,
            Rating = checked((byte)request.Rating.GetValueOrDefault()),
            IdStatus = await db.ArticleFeedbackStatuses
                .AsNoTracking()
                .Where(x => x.Code == "pending")
                .Select(x => x.Id)
                .SingleAsync(ct),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AccountArticleFeedback.Add(feedback);
        await db.SaveChangesAsync(ct);

        return Ok(new { saved = true, id = feedback.Id, status = "pending" });
    }
}
