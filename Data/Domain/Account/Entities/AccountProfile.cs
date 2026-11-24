using Shop.Data.Domain.Store.Entities;

namespace Shop.Data.Domain.Account.Entities;

public sealed class AccountProfile
{
    public decimal Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int? IdGender { get; set; }
    public AccountGender? Gender { get; set; }
    public bool NewsletterEnabled { get; set; }
    public string? Password { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Shop.Data.Domain.Store.Entities.ProductFeedback> ProductFeedback { get; set; } =
        new List<Shop.Data.Domain.Store.Entities.ProductFeedback>();
}

public sealed class AccountGender
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameFa { get; set; } = string.Empty;
}

public sealed class AccountCart
{
    public decimal Id { get; set; }
    public decimal IdProfile { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<AccountCartItem> Items { get; set; } = new List<AccountCartItem>();
}

public sealed class AccountCartItem
{
    public decimal Id { get; set; }
    public decimal IdCart { get; set; }
    public decimal IdProduct { get; set; }
    public long? IdVariant { get; set; }
    public long IdSalesPlan { get; set; }
    public int Quantity { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<AccountCartItemAccessory> Accessories { get; set; } = new List<AccountCartItemAccessory>();
}

public sealed class AccountCartItemAccessory
{
    public decimal Id { get; set; }
    public decimal IdCartItem { get; set; }
    public decimal IdProductAccessory { get; set; }
}

public sealed class AccountAddress
{
    public decimal Id { get; set; }
    public decimal IdProfile { get; set; }
    public string Title { get; set; } = "";
    public string Recipient { get; set; } = "";
    public string Phone { get; set; } = "";
    public long IdCountry { get; set; }
    public long IdProvince { get; set; }
    public long IdCity { get; set; }
    public string Street { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AccountFavorite
{
    public long Id { get; set; }
    public decimal IdProfile { get; set; }
    public decimal IdProduct { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AccountCompareItem
{
    public decimal Id { get; set; }
    public decimal IdProfile { get; set; }
    public decimal IdProduct { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AccountPriceAlert
{
    public long Id { get; set; }
    public decimal IdProfile { get; set; }
    public decimal IdProduct { get; set; }
    public string Scope { get; set; } = "";
    public string VariantSignature { get; set; } = "";
    public string? VariantsJson { get; set; }
    public decimal PriceAtRegistration { get; set; }
    public string? StockAtRegistration { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AccountArticleFeedback
{
    public decimal Id { get; set; }
    public decimal IdProfile { get; set; }
    public decimal IdMagazineArticle { get; set; }
    public string DisplayName { get; set; } = "";
    public string FeedbackText { get; set; } = "";
    public byte? Rating { get; set; }
    public int IdStatus { get; set; }
    public ArticleFeedbackStatusReference StatusReference { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class StoreOrderPreviousNumber
{
    public decimal IdOrder { get; set; }
    public string PreviousNumber { get; set; } = string.Empty;
}

public sealed class StoreCustomerOrderReference
{
    public decimal Id { get; set; }
    public decimal IdProfile { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int IdStatus { get; set; }
    public OrderStatusReference StatusReference { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal? IdCoupon { get; set; }
    public decimal CouponDiscountAmount { get; set; }
    public decimal IdAddress { get; set; }
    public string PaymentMethod { get; set; } = "";
    public long IdBank { get; set; }
    public StoreBank Bank { get; set; } = null!;
    public string? ShippingCarrier { get; set; }
    public string? ShippingTrackingCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int ItemCount { get; set; }
    public string AddressSnapshotJson { get; set; } = "{}";
    public decimal? IdVoucher { get; set; }
    public ICollection<StoreCustomerOrderItem> Items { get; set; } = new List<StoreCustomerOrderItem>();
}

public sealed class StoreOrderShipmentTrackingHistory
{
    public long Id { get; set; }
    public decimal IdOrder { get; set; }
    public string? PreviousCarrier { get; set; }
    public string? PreviousTrackingCode { get; set; }
    public string Carrier { get; set; } = "";
    public string TrackingCode { get; set; } = "";
    public string ActorUserKey { get; set; } = "";
    public DateTime ChangedAtUtc { get; set; }
}

public sealed class StoreCustomerOrderItem
{
    public decimal Id { get; set; }
    public decimal IdOrder { get; set; }
    public decimal IdProduct { get; set; }
    public long? IdVariant { get; set; }
    public long IdSalesPlan { get; set; }
    public int Quantity { get; set; }
    public decimal IdPriceVersion { get; set; }
    public StorePriceVersion PriceVersion { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class StorePaymentAttempt
{
    public decimal Id { get; set; }
    public decimal IdOrder { get; set; }
    public decimal Amount { get; set; }
    public int IdStatus { get; set; }
    public PaymentAttemptStatusReference StatusReference { get; set; } = null!;
    public string? GatewayReference { get; set; }
    public string? ReceiptReference { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class AccountCoupon
{
    public decimal Id { get; set; }
    public decimal? IdProfile { get; set; }
    public string CouponKey { get; set; } = "";
    public string Code { get; set; } = "";
    public int DiscountPercent { get; set; }
    public int UsageLimit { get; set; }
    public bool Enabled { get; set; }
    public DateOnly ExpiresOn { get; set; }
}

public sealed class StoreCouponUsage
{
    public decimal Id { get; set; }
    public decimal IdCoupon { get; set; }
    public decimal IdCustomerOrder { get; set; }
}
