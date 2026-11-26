using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreMagazineAdminData(ShopDbContext db) : IStoreMagazineAdminData
{
    public async Task<IReadOnlyList<MagazineArticleListData>> ReadArticlesAsync(CancellationToken cancellationToken) =>
        await db.MagazineArticles.AsNoTracking().OrderByDescending(article => article.UpdatedAtUtc ?? article.CreatedAtUtc)
            .Select(article => new MagazineArticleListData(article.Id, article.ArticleKey, article.Title,
                article.Category.CategoryKey, article.IsPublished, article.Sections.Count))
            .ToListAsync(cancellationToken);

    public async Task<MagazineArticleEditData?> ReadArticleAsync(long id, CancellationToken cancellationToken) =>
        await db.MagazineArticles.AsNoTracking().Where(article => article.Id == id)
            .Select(article => new MagazineArticleEditData(article.ArticleKey, article.Category.CategoryKey,
                article.Title, article.Summary, article.Author, article.Kicker, article.Branch.BranchKey,
                article.Topic.TopicKey, article.Area.AreaKey, article.HeroImagePath, article.ContentImagePath,
                article.ReadTimeMinutes, article.ReadTimeText, article.Lead, article.Conclusion, article.IsPublished,
                article.Sections.OrderBy(section => section.SortOrder)
                    .Select(section => new MagazineArticleSectionData(section.Heading, section.Body)).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<MagazineArticleSaveResult> SaveArticleAsync(long? id, MagazineArticleUpdate update,
        CancellationToken cancellationToken)
    {
        if (await db.MagazineArticles.AnyAsync(article => article.ArticleKey == update.ArticleKey &&
                article.Id != (id ?? 0), cancellationToken))
            return new(MagazineArticleSaveResultKind.DuplicateKey);
        var category = await db.MagazineArticleCategories.SingleOrDefaultAsync(item =>
            item.CategoryKey == update.Category, cancellationToken) ?? new MagazineArticleCategory
            { CategoryKey = update.Category };
        var branch = await db.MagazineArticleBranches.SingleOrDefaultAsync(item => item.BranchKey == update.Branch,
            cancellationToken) ?? new MagazineArticleBranch { BranchKey = update.Branch };
        var topic = await db.MagazineArticleTopics.SingleOrDefaultAsync(item => item.TopicKey == update.Topic,
            cancellationToken) ?? new MagazineArticleTopic { TopicKey = update.Topic };
        var area = await db.MagazineArticleAreas.SingleOrDefaultAsync(item => item.AreaKey == update.Area,
            cancellationToken) ?? new MagazineArticleArea { AreaKey = update.Area };
        if (category.Id == 0) db.MagazineArticleCategories.Add(category);
        if (branch.Id == 0) db.MagazineArticleBranches.Add(branch);
        if (topic.Id == 0) db.MagazineArticleTopics.Add(topic);
        if (area.Id == 0) db.MagazineArticleAreas.Add(area);
        var article = id is null
            ? new MagazineArticle { CreatedAtUtc = DateTime.UtcNow }
            : await db.MagazineArticles.Include(item => item.Sections)
                .SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (article is null) return new(MagazineArticleSaveResultKind.NotFound);
        article.ArticleKey = update.ArticleKey;
        article.Category = category;
        article.Branch = branch;
        article.Topic = topic;
        article.Area = area;
        article.Title = update.Title;
        article.Summary = update.Summary;
        article.Author = Clean(update.Author);
        article.Kicker = Clean(update.Kicker);
        article.HeroImagePath = Clean(update.HeroImagePath);
        article.ContentImagePath = Clean(update.ContentImagePath);
        article.ReadTimeMinutes = update.ReadTimeMinutes;
        article.ReadTimeText = Clean(update.ReadTimeText);
        article.Lead = Clean(update.Lead);
        article.Conclusion = Clean(update.Conclusion);
        article.IsPublished = update.IsPublished;
        article.UpdatedAtUtc = DateTime.UtcNow;
        article.Sections.Clear();
        int order = 0;
        foreach (var section in update.Sections)
            article.Sections.Add(new MagazineArticleSection
                { Heading = section.Heading, Body = section.Body, SortOrder = order++ });
        if (id is null) db.MagazineArticles.Add(article);
        await db.SaveChangesAsync(cancellationToken);
        return new(MagazineArticleSaveResultKind.Saved, article.Id);
    }

    public async Task<MagazineArticleDeleteResult> DeleteArticleAsync(long id, CancellationToken cancellationToken)
    {
        var article = await db.MagazineArticles.Include(item => item.Sections)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (article is null) return MagazineArticleDeleteResult.NotFound;
        if (await db.AccountArticleInteractions.AnyAsync(item => item.IdMagazineArticle == id, cancellationToken))
            return MagazineArticleDeleteResult.HasInteractions;
        db.MagazineArticles.Remove(article);
        await db.SaveChangesAsync(cancellationToken);
        return MagazineArticleDeleteResult.Deleted;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
