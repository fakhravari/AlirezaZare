namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreNewsletterSubscriber
{
    public decimal Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime SubscribedAtUtc { get; set; }
    public DateTime? UnsubscribedAtUtc { get; set; }
}