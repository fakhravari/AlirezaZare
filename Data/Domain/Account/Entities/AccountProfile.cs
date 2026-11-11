namespace Shop.Data.Domain.Account.Entities;

public sealed class AccountProfile
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public bool NewsletterEnabled { get; set; }
    public string? PasswordHash { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Shop.Data.Domain.Store.Entities.ProductFeedback> ProductFeedback { get; set; } =
        new List<Shop.Data.Domain.Store.Entities.ProductFeedback>();
}

public sealed class AccountCart
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<AccountCartItem> Items { get; set; } = new List<AccountCartItem>();
}

public sealed class AccountCartItem
{
    public long Id { get; set; }
    public long IdCart { get; set; }
    public long IdProduct { get; set; }
    public long? IdProductVariant { get; set; }
    public long IdSalesPlan { get; set; }
    public int Quantity { get; set; }
    public string OptionsJson { get; set; } = "{}";
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class AccountAddress
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
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
    public long IdProfile { get; set; }
    public long IdProduct { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AccountCompareItem
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
    public long IdProduct { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AccountPriceAlert
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
    public long IdProduct { get; set; }
    public string Scope { get; set; } = "";
    public string VariantSignature { get; set; } = "";
    public string? VariantsJson { get; set; }
    public decimal PriceAtRegistration { get; set; }
    public string? StockAtRegistration { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AccountArticleFeedback
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
    public long IdMagazineArticle { get; set; }
    public string DisplayName { get; set; } = "";
    public string FeedbackText { get; set; } = "";
    public byte? Rating { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AccountCheckoutPreference
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
    public long? IdSelectedAddress { get; set; }
    public string PaymentMethod { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class StoreCustomerOrderReference
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public long? IdAddress { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? ExternalOrderKey { get; set; }
    public string? ShippingCarrier { get; set; }
    public string? ShippingTrackingCode { get; set; }
    public DateTime OrderedAtUtc { get; set; }
    public int ItemCount { get; set; }
    public string AddressSnapshotJson { get; set; } = "{}";
    public long? IdWarehouseDocument { get; set; }
    public ICollection<StoreCustomerOrderItem> Items { get; set; } = new List<StoreCustomerOrderItem>();
}

public sealed class StoreOrderShippingHistory
{
    public long Id { get; set; }
    public long IdOrder { get; set; }
    public string PreviousStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
    public string ActorUserKey { get; set; } = "";
    public DateTime ChangedAtUtc { get; set; }
}

public sealed class StoreOrderShipmentTrackingHistory
{
    public long Id { get; set; }
    public long IdOrder { get; set; }
    public string? PreviousCarrier { get; set; }
    public string? PreviousTrackingCode { get; set; }
    public string Carrier { get; set; } = "";
    public string TrackingCode { get; set; } = "";
    public string ActorUserKey { get; set; } = "";
    public DateTime ChangedAtUtc { get; set; }
}

public sealed class StoreCustomerOrderItem
{
    public long Id { get; set; }
    public long IdCustomerOrder { get; set; }
    public long IdProduct { get; set; }
    public long? IdProductVariant { get; set; }
    public string ProductNameSnapshot { get; set; } = "";
    public long IdSalesPlan { get; set; }
    public int Quantity { get; set; }
    public decimal UnitAmount { get; set; }
    public string OptionsJson { get; set; } = "{}";
}

public sealed class AccountProductFeedback
{
    public long Id { get; set; }
    public long IdProfile { get; set; }
    public long IdProduct { get; set; }
    public string FeedbackType { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string FeedbackText { get; set; } = "";
    public int? Rating { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class StorePaymentAttempt
{
    public Guid Id { get; set; }
    public long IdOrder { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string? GatewayReference { get; set; }
    public string? ReceiptReference { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class WarehousePaidOrderInbox
{
    public long Id { get; set; }
    public string StoreOrderKey { get; set; } = "";
    public string CustomerKey { get; set; } = "";
    public DateTime PaymentAtUtc { get; set; }
    public string Status { get; set; } = "P";
    public ICollection<WarehousePaidOrderInboxLine> Lines { get; set; } = new List<WarehousePaidOrderInboxLine>();
}

public sealed class WarehousePaidOrderInboxLine
{
    public long Id { get; set; }
    public long IdPaidOrderInbox { get; set; }
    public long IdInventoryItem { get; set; }
    public long IdUnit { get; set; }
    public long IdSalesPlanReference { get; set; }
    public decimal Quantity { get; set; }
    public decimal ConversionFactor { get; set; }
    public decimal BaseQuantity { get; set; }
}

public sealed class WarehousePaidOrderInboxDocument
{
    public long Id { get; set; }
    public long IdPaidOrderInbox { get; set; }
    public long IdWarehouseDocument { get; set; }
}
