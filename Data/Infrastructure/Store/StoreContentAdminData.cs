using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreContentAdminData(ShopDbContext db) : IStoreContentAdminData
{
    private static readonly string[] EditablePageKeys =
        ["about", "contact", "shipping", "warranty", "tracking", "terms", "returns", "privacy", "faq"];

    public async Task<IReadOnlyList<ManagedPageListRow>> ReadManagedPagesAsync(CancellationToken cancellationToken) =>
        await db.StoreContentPages.AsNoTracking().Where(page => EditablePageKeys.Contains(page.PageKey))
            .OrderBy(page => page.Title).Select(page => new ManagedPageListRow(page.PageKey, page.RoutePath,
                page.Title, page.IsPublished, page.Sections.Count)).ToListAsync(cancellationToken);

    public async Task<ManagedPageEditData?> ReadManagedPageAsync(string pageKey,
        CancellationToken cancellationToken)
    {
        if (!EditablePageKeys.Contains(pageKey)) return null;
        var page = await db.StoreContentPages.AsNoTracking().Where(item => item.PageKey == pageKey)
            .Select(item => new ManagedPageEditData(item.Title, item.Description ?? "", item.IsPublished,
                item.Sections.OrderBy(section => section.SortOrder).ThenBy(section => section.Id)
                    .Select(section => new ManagedPageSectionEditData(section.SectionKey, section.Heading,
                        section.Body ?? "", section.IsPublished)).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);
        return page;
    }

    public async Task<bool> SaveManagedPageAsync(string pageKey, string title, string description, bool published,
        IReadOnlyList<ManagedPageSectionEditData> sections, CancellationToken cancellationToken)
    {
        if (!EditablePageKeys.Contains(pageKey)) return false;
        var page = await db.StoreContentPages.Include(item => item.Sections)
            .SingleOrDefaultAsync(item => item.PageKey == pageKey, cancellationToken);
        if (page is null) return false;
        page.Title = title;
        page.Description = description;
        page.ContentSource = "Database";
        page.IsPublished = published;
        page.UpdatedAtUtc = DateTime.UtcNow;
        page.Sections.Clear();
        for (int index = 0; index < sections.Count; index++)
        {
            var section = sections[index];
            page.Sections.Add(new StoreContentPageSection
            {
                SectionKey = section.SectionKey, Heading = section.Heading, Body = section.Body,
                SortOrder = index, IsPublished = true
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CategoryContentData?> ReadCategoryContentAsync(string key, CancellationToken cancellationToken)
    {
        var category = await db.StoreCategories.AsNoTracking().Where(item => item.CategoryKey == key)
            .Select(item => new CategoryContentData(item.ProductIntro ?? "", item.ProductHighlightsJson ?? "[]",
                item.ProductSpecsJson ?? "[]")).SingleOrDefaultAsync(cancellationToken);
        return category;
    }

    public async Task<bool> SaveCategoryContentAsync(string key, string intro, string highlights, string specs,
        CancellationToken cancellationToken)
    {
        var category = await db.StoreCategories.SingleOrDefaultAsync(item => item.CategoryKey == key,
            cancellationToken);
        if (category is null) return false;
        category.ProductIntro = intro;
        category.ProductHighlightsJson = highlights;
        category.ProductSpecsJson = specs;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<StoreFaqAdminRow>> ReadFaqItemsAsync(CancellationToken cancellationToken) =>
        await db.StoreFaqItems.AsNoTracking()
            .Where(item => db.StoreContentPages.Any(page => page.Id == item.IdContentPage && page.PageKey == "faq"))
            .OrderBy(item => item.SortOrder).ThenBy(item => item.Id)
            .Select(item => new StoreFaqAdminRow(item.Id, item.Question, item.Answer, item.LinkText, item.LinkUrl,
                item.SortOrder, item.IsPublished)).ToListAsync(cancellationToken);

    public async Task<StoreFaqSaveResult> SaveFaqAsync(long? id, StoreFaqAdminEdit edit,
        CancellationToken cancellationToken)
    {
        var item = id.HasValue
            ? await db.StoreFaqItems.SingleOrDefaultAsync(row => row.Id == id &&
                db.StoreContentPages.Any(page => page.Id == row.IdContentPage && page.PageKey == "faq"),
                cancellationToken)
            : null;
        if (id.HasValue && item is null) return StoreFaqSaveResult.NotFound;
        if (!id.HasValue)
        {
            var page = await db.StoreContentPages.SingleOrDefaultAsync(row => row.PageKey == "faq",
                cancellationToken);
            if (page is null) return StoreFaqSaveResult.FaqPageMissing;
            item = new StoreFaqItem { IdContentPage = page.Id };
            db.StoreFaqItems.Add(item);
        }
        item!.Question = edit.Question;
        item.Answer = edit.Answer;
        item.LinkText = edit.LinkText;
        item.LinkUrl = edit.LinkUrl;
        item.SortOrder = edit.SortOrder;
        item.IsPublished = edit.Published;
        await db.SaveChangesAsync(cancellationToken);
        return StoreFaqSaveResult.Saved;
    }

    public async Task<bool> DeleteFaqAsync(long id, CancellationToken cancellationToken)
    {
        var item = await db.StoreFaqItems.SingleOrDefaultAsync(row => row.Id == id &&
            db.StoreContentPages.Any(page => page.Id == row.IdContentPage && page.PageKey == "faq"),
            cancellationToken);
        if (item is null) return false;
        db.StoreFaqItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
