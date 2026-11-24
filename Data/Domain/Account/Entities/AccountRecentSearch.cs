namespace Shop.Data.Domain.Account.Entities;

public sealed class AccountRecentSearch
{
    public decimal Id { get; set; }
    public decimal IdProfile { get; set; }
    public string SearchKey { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public DateTime SearchedAtUtc { get; set; }
}