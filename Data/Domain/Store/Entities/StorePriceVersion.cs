namespace Shop.Data.Domain.Store.Entities;

public static class StorePriceKinds
{
    public const string Product = "product";
    public const string Variant = "variant";
    public const string Accessory = "accessory";
    public const string Option = "option";
    public const string OrderSnapshot = "order-snapshot";
}

public static class StorePriceScopes
{
    public static string Product(long productId, long planId) => $"product/{productId}/plan/{planId}";
    public static string Variant(long variantId, long planId) => $"variant/{variantId}/plan/{planId}";
    public static string Accessory(long productId, long accessoryId, long planId) => $"accessory/{productId}/{accessoryId}/plan/{planId}";
    public static string Option(long optionValueId) => $"option/{optionValueId}";
    public static string LegacyOrderSnapshot(long orderItemId) => $"order-snapshot/{orderItemId}";
}

/// <summary>A historical price value. Exactly one version per scope is active at a time.</summary>
public sealed class StorePriceVersion
{
    public long Id { get; set; }
    public string ScopeKey { get; set; } = string.Empty;
    public long IdProduct { get; set; }
    public long? IdSalesPlan { get; set; }
    public string PriceKind { get; set; } = string.Empty;
    public long? TargetId { get; set; }
    public decimal PriceAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? ChangedBy { get; set; }
    public string? ChangeReason { get; set; }
    public long? PreviousVersionId { get; set; }
    public StorePriceVersion? PreviousVersion { get; set; }
}

/// <summary>The exact price versions and applied components captured on an order line.</summary>
public sealed class StoreOrderItemPriceReference
{
    public long IdOrderItem { get; set; }
    public long IdPriceVersion { get; set; }
    public StorePriceVersion PriceVersion { get; set; } = null!;
}
