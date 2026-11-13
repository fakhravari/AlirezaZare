namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreNewsletterSubscriber
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime SubscribedAtUtc { get; set; }
}
