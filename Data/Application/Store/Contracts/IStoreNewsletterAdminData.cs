namespace Shop.Data.Application.Store.Contracts;

public interface IStoreNewsletterAdminData
{
    Task<int> CountAsync(string search, bool includeUnsubscribed, CancellationToken cancellationToken);
    Task<IReadOnlyList<NewsletterSubscriberData>> ReadPageAsync(string search, bool includeUnsubscribed, int offset,
        int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<NewsletterSubscriberData>> ReadExportBatchAsync(string search, bool includeUnsubscribed,
        decimal afterId, int batchSize, CancellationToken cancellationToken);
    Task<bool> SetSubscribedAsync(long id, bool subscribed, CancellationToken cancellationToken);
}

public sealed record NewsletterSubscriberData(decimal Id, string Email, DateTime SubscribedAtUtc,
    DateTime? UnsubscribedAtUtc);
