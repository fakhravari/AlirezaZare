namespace Shop.Database.Domain.Account.Entities;

public sealed class AccountProfile
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Shop.Database.Domain.Store.Entities.ProductFeedback> ProductFeedback { get; set; } =
        new List<Shop.Database.Domain.Store.Entities.ProductFeedback>();
}

public sealed class AccountCartReference
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
}

public sealed class AccountCustomerOrderReference
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
}

public sealed class StoreCustomerOrderReference
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
}
