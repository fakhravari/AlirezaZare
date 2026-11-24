namespace Shop.Data.Domain.Account.Entities;

public sealed class AccountArticleInteraction
{
    public long Id { get; set; }
    public decimal IdProfile { get; set; }
    public decimal IdMagazineArticle { get; set; }
    public string InteractionType { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}