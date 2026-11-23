namespace Shop.Data.Domain.Store.Entities;

public sealed class OrderStatusReference
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public sealed class PaymentAttemptStatusReference
{
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public sealed class ContactMessageStatusReference
{
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public sealed class ArticleFeedbackStatusReference
{
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public sealed class WarehouseDocumentStatusReference
{
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}
