namespace Shop.Controllers;

    public sealed record ProfileRequest(
        string FirstName,
        string LastName,
        string? Phone,
        int? IdGender = null,
        bool Newsletter = false);

    public sealed record PasswordChangeRequest(
        string? CurrentPassword,
        string? NewPassword,
        string? NewPasswordConfirmation);

    public sealed record CartRequest(IReadOnlyList<CartLineRequest>? Items);

    public sealed record CartLineRequest(
        decimal ProductId,
        int Quantity,
        string SalesPlanId,
        System.Text.Json.JsonElement? Options,
        long? VariantId = null,
        IReadOnlyList<decimal>? AccessoryIds = null);

    public sealed record CheckoutRequest(long AddressId, string PaymentMethod, string? CouponCode = null);

    public sealed record CouponRequest(string? Code);

    public sealed record ListRequest(IReadOnlyList<decimal>? ProductIds);

    public sealed record PriceAlertListRequest(IReadOnlyList<PriceAlertRequest>? Items);

    public sealed record PriceAlertRequest(
        string? ProductId,
        string? Scope,
        System.Text.Json.JsonElement? Variants,
        decimal PriceAtRegistration,
        string? StockAtRegistration,
        DateTimeOffset CreatedAt);

    public sealed record ArticleFeedbackRequest(string? Name, string? Text, byte? Rating);

    public sealed record RecentSearchRequest(string? Query);

    public sealed record AddressRequest(
        string? Title,
        string? Recipient,
        string? Phone,
        long? CountryId,
        long? ProvinceId,
        long? CityId,
        string? Street,
        string? Postal);
