namespace Shop.Database.Domain.Account.Entities;

public sealed class AccountProfile
{
    public long Id { get; set; }
    public ICollection<Shop.Database.Domain.Store.Entities.ProductFeedback> ProductFeedback { get; set; } =
        new List<Shop.Database.Domain.Store.Entities.ProductFeedback>();
}
