using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreNewsletterAdminData(ShopDbContext db) : IStoreNewsletterAdminData
{
    private IQueryable<Shop.Data.Domain.Store.Entities.StoreNewsletterSubscriber> Query(string search,
        bool includeUnsubscribed)
    {
        var query = db.StoreNewsletterSubscribers.AsNoTracking();
        if (!includeUnsubscribed) query = query.Where(item => item.UnsubscribedAtUtc == null);
        if (search.Length > 0) query = query.Where(item => item.Email.Contains(search));
        return query;
    }

    public Task<int> CountAsync(string search, bool includeUnsubscribed, CancellationToken cancellationToken) =>
        Query(search, includeUnsubscribed).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<NewsletterSubscriberData>> ReadPageAsync(string search, bool includeUnsubscribed,
        int offset, int pageSize, CancellationToken cancellationToken) => await Query(search, includeUnsubscribed)
        .OrderByDescending(item => item.SubscribedAtUtc).ThenByDescending(item => item.Id)
        .Skip(offset).Take(pageSize)
        .Select(item => new NewsletterSubscriberData(item.Id, item.Email, item.SubscribedAtUtc,
            item.UnsubscribedAtUtc)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NewsletterSubscriberData>> ReadExportBatchAsync(string search,
        bool includeUnsubscribed, decimal afterId, int batchSize, CancellationToken cancellationToken) =>
        await Query(search, includeUnsubscribed).Where(item => item.Id > afterId).OrderBy(item => item.Id)
            .Take(batchSize).Select(item => new NewsletterSubscriberData(item.Id, item.Email, item.SubscribedAtUtc,
                item.UnsubscribedAtUtc)).ToListAsync(cancellationToken);

    public async Task<bool> SetSubscribedAsync(long id, bool subscribed, CancellationToken cancellationToken)
    {
        var item = await db.StoreNewsletterSubscribers.SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (item is null) return false;
        item.UnsubscribedAtUtc = subscribed ? null : DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
