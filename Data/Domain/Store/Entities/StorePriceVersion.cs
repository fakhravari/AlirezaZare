namespace Shop.Data.Domain.Store.Entities;

public static class StorePriceVersionReference
{
    public static string GetComponent(long? idProductVariant, decimal? idAccessory) =>
        idAccessory.HasValue ? "accessory" :
        idProductVariant.HasValue ? "variant" : "product";

    public static bool AppliesDiscount(decimal? idAccessory) => !idAccessory.HasValue;
}

/// <summary>A historical price value. Exactly one version per typed target is active at a time.</summary>
public sealed class StorePriceVersion
{
    public decimal Id { get; set; }
    public decimal IdProduct { get; set; }
    public long IdSalesPlan { get; set; }
    public long? IdVariant { get; set; }
    public decimal? IdAccessory { get; set; }
    public decimal PriceBeforeDiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal FinalPriceAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long IdChangedBy { get; set; }
    public string? ChangeReason { get; set; }
    public decimal? IdPreviousVersion { get; set; }
    public StorePriceVersion? PreviousVersion { get; set; }
}

/// <summary>Accessory selections shared by all lines of one order, product and sales plan.</summary>
public sealed class StoreOrderAccessory
{
    public decimal IdOrder { get; set; }
    public decimal IdProduct { get; set; }
    public long IdSalesPlan { get; set; }
    public decimal IdAccessory { get; set; }
    public decimal IdPriceVersion { get; set; }
    public string AccessoryNameSnapshot { get; set; } = "";
    public decimal UnitAmountSnapshot { get; set; }
    public StorePriceVersion PriceVersion { get; set; } = null!;
}

/// <summary>Order line unit amount calculated from its price version references.</summary>
public sealed class StoreOrderItemAmount
{
    public long IdOrderItem { get; set; }
    public decimal UnitAmount { get; set; }
}
