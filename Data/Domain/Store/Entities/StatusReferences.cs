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
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public sealed class ContactMessageStatusReference
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public sealed class ArticleFeedbackStatusReference
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public sealed class VoucherStatus
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}
