using Shop.Data.Application.Admin.Contracts;
using Shop.Data.Helpers;

namespace Shop.Data.Application.Store.Contracts;

public interface IStoreVariantPricesAdminData
{
    Task<AdminOperationResult> Plans(string? q, DateOnly? from, DateOnly? to,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> PricePlans(CancellationToken cancellationToken);
    Task<AdminOperationResult> Products(string? q, int page = 1,
        CancellationToken cancellationToken = default);
    Task<AdminOperationResult> PriceHistory(decimal? productId, decimal[]? productIds,
        long? salesPlanId, string? q, DateOnly? from, DateOnly? to,
        int take = 100, CancellationToken cancellationToken = default);
    Task<AdminOperationResult> CreatePlan(PlanInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> UpdatePlan(long id, PlanInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> SetPlanPublication(long id, PlanPublicationInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> DeletePlan(long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> SavePlan(long id, PlanSettingsInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> List(
        string? q,
        decimal[]? productIds,
        long? salesPlanId,
        DateOnly? from,
        DateOnly? to,
        bool all = false,
        int page = 1,
        int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default);
    Task<AdminOperationResult> Save(long variantId, string salesPlanId, VariantPlanPriceInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> ProductOffers(
        string? q,
        decimal[]? productIds,
        DateOnly? from,
        DateOnly? to,
        int page = 1,
        int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default);
    Task<AdminOperationResult> SaveProductOffer(long productId, string salesPlanId,
        ProductOfferInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> OfferAccessories(decimal productId, string salesPlanId,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> SaveOfferAccessory(decimal productId, string salesPlanId, decimal accessoryId,
        OfferAccessoryInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> OfferSpecifications(decimal productId, string salesPlanId,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> SaveOfferSpecifications(decimal productId, string salesPlanId,
        List<OfferSpecification> input, CancellationToken cancellationToken);
}

public sealed record VariantPlanPriceInput(decimal PriceAmount, decimal DiscountPercent, bool IsEnabled);

public sealed record PlanSettingsInput(
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string? Label,
    bool ShowCountdown);

public sealed record PlanInput(
    string? Key,
    string? Code,
    string? DisplayName,
    string? PlanType,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string? Label,
    bool ShowCountdown);

public sealed record PlanPublicationInput(bool IsActive);

public sealed record ProductOfferInput(
    decimal PriceAmount,
    decimal DiscountPercent,
    int MaxPurchaseQuantity,
    bool IsEnabled);

public sealed record OfferAccessoryInput(decimal PriceAmount, bool IsAllowed);

public sealed class OfferSpecification
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}
