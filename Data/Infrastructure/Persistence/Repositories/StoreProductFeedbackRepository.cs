using System.Data;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Application.Store.Contracts;
using Shop.Database.Application.Store.Models;
using Shop.Database.Domain.Store.Entities;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Database.Infrastructure.Persistence.Repositories;

public sealed class StoreProductFeedbackRepository(ShopDbContext dbContext) : IProductFeedbackRepository
{
    public async Task<ProductFeedbackSnapshot?> ReadAsync(int productId, string type, long? profileId,
        CancellationToken cancellationToken)
    {
        long? idProduct = await ActiveProductIdAsync(productId, cancellationToken);
        if (idProduct is null) return null;

        IQueryable<ProductFeedback> approved = dbContext.ProductFeedback.AsNoTracking()
            .Where(x => x.IdProduct == idProduct && x.FeedbackType == type && x.Status == "approved");
        long totalCount = await approved.LongCountAsync(cancellationToken);
        decimal? averageRating = type == "reviews"
            ? await approved.Where(x => x.Rating.HasValue).Select(x => (decimal?)x.Rating!.Value)
                .AverageAsync(cancellationToken)
            : null;

        List<ProductFeedback> rows = await dbContext.ProductFeedback.AsNoTracking()
            .Where(x => x.IdProduct == idProduct && x.FeedbackType == type &&
                        (x.Status == "approved" ||
                         (x.Status == "pending" && profileId.HasValue && x.IdProfile == profileId)))
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Take(500).ToListAsync(cancellationToken);

        ProductFeedbackItem[] items = rows.Select(x => new ProductFeedbackItem(
            x.Id, x.DisplayName, x.FeedbackText, x.Rating, x.Status, x.AdminReply,
            new DateTimeOffset(DateTime.SpecifyKind(x.CreatedAtUtc, DateTimeKind.Utc)),
            x.RepliedAtUtc is DateTime repliedAt
                ? new DateTimeOffset(DateTime.SpecifyKind(repliedAt, DateTimeKind.Utc))
                : null, x.IsSample)).ToArray();
        return new ProductFeedbackSnapshot(items, totalCount, averageRating);
    }

    public async Task<long?> AddAsync(int productId, long? profileId, string type, string name, string text,
        byte? rating, CancellationToken cancellationToken)
    {
        long? idProduct = await ActiveProductIdAsync(productId, cancellationToken);
        if (idProduct is null) return null;

        var feedback = new ProductFeedback
        {
            IdProduct = idProduct.Value,
            IdProfile = profileId,
            FeedbackType = type,
            DisplayName = name,
            FeedbackText = text,
            Rating = rating,
            Status = "pending",
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.ProductFeedback.Add(feedback);
        await dbContext.SaveChangesAsync(cancellationToken);
        return feedback.Id;
    }

    public async Task<int?> ImportLegacyAsync(int productId, long? profileId, string type,
        IReadOnlyList<ProductFeedbackLegacyItem> items, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        long? idProduct = await ActiveProductIdAsync(productId, cancellationToken);
        if (idProduct is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        List<ProductFeedback> existing = await dbContext.ProductFeedback
            .Where(x => x.IdProduct == idProduct && x.FeedbackType == type)
            .ToListAsync(cancellationToken);
        var known = existing.Select(x => (x.DisplayName, x.FeedbackText, x.CreatedAtUtc)).ToHashSet();
        var knownWithoutDate = existing.Select(x => (x.DisplayName, x.FeedbackText)).ToHashSet();
        var imported = new List<ProductFeedback>();

        foreach (ProductFeedbackLegacyItem item in items)
        {
            DateTime? sourceDate = item.CreatedAt?.UtcDateTime;
            DateTime createdAt = sourceDate is null ? DateTime.UtcNow : TruncateToMilliseconds(sourceDate.Value);
            bool duplicate = sourceDate is null
                ? knownWithoutDate.Contains((item.Name, item.Text))
                : known.Contains((item.Name, item.Text, createdAt));
            if (duplicate) continue;

            var feedback = new ProductFeedback
            {
                IdProduct = idProduct.Value,
                IdProfile = profileId,
                FeedbackType = type,
                DisplayName = item.Name,
                FeedbackText = item.Text,
                Rating = type == "reviews" ? item.Rating : null,
                Status = "pending",
                CreatedAtUtc = createdAt
            };
            imported.Add(feedback);
            known.Add((feedback.DisplayName, feedback.FeedbackText, feedback.CreatedAtUtc));
            if (sourceDate is null) knownWithoutDate.Add((feedback.DisplayName, feedback.FeedbackText));
        }

        if (imported.Count > 0)
        {
            dbContext.ProductFeedback.AddRange(imported);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return imported.Count;
    }

    private Task<long?> ActiveProductIdAsync(int productId, CancellationToken cancellationToken) =>
        dbContext.Products.AsNoTracking()
            .Where(x => x.Id == productId && x.IsCatalogActive && x.IsStoreEnabled)
            .Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

    private static DateTime TruncateToMilliseconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
}
