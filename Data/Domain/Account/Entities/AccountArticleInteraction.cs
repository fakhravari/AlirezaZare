namespace Shop.Data.Domain.Account.Entities;

public sealed class AccountArticleInteraction
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
    public long IdMagazineArticle { get; set; }
    public string InteractionType { get; set; } = "";
}
