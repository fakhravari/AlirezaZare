namespace Shop.Database.Domain.Account.Entities;

public sealed class AccountRecentSearch
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
    public string SearchKey { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public DateTime SearchedAtUtc { get; set; }
}
