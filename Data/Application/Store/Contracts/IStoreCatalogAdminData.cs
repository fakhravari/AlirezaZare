using Shop.Data.Application.Admin.Contracts;
using Shop.Data.Helpers;

namespace Shop.Data.Application.Store.Contracts;

public interface IStoreCatalogAdminData
{
    Task<AdminOperationResult> ProductAccessories(long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> CreateProductAccessory(long id, ProductAccessoryInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> UpdateProductAccessory(long id, long linkId,
        ProductAccessoryInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> ProductEditor(long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> ProductDetails(long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> UpdateProduct(long id, ProductEditorInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> CreateProductBadge(ProductBadgeInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> ProductBenefits(long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> CreateProductBenefit(long id, ProductBenefitInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> UpdateProductBenefit(long id, long benefitId, ProductBenefitInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> DeleteProductBenefit(long id, long benefitId, CancellationToken cancellationToken);
    Task<AdminOperationResult> ProductDrafts(
        string? q,
        int page = 1,
        int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default);
    Task<AdminOperationResult> ProductCreateOptions(CancellationToken cancellationToken);
    Task<AdminOperationResult> CreateProduct(ProductCreateInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> PublishProduct(long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> UnpublishProduct(long id, CancellationToken cancellationToken);
    Task<AdminOperationResult> BrandCountries(CancellationToken cancellationToken);
    Task<AdminOperationResult> CreateBrand(BrandInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> UpdateBrand(string key, BrandInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> CategoryOptions(CancellationToken cancellationToken);
    Task<AdminOperationResult> CreateCategory(CategoryInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> UpdateCategory(string key, CategoryInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> AttributeEditor(string key, CancellationToken cancellationToken);
    Task<AdminOperationResult> CreateAttribute(AttributeDefinitionInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> UpdateAttribute(string key, AttributeDefinitionInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> SetAttributeStatus(string key, CatalogStatusInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> SetCategoryStatus(string key, CatalogStatusInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> SetBrandStatus(string key, CatalogStatusInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> GetSection(
        string section,
        string? q,
        int page = 1,
        int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default);
}

public sealed record ProductAccessoryInput(
    string Name,
    string? Description,
    decimal PriceAmount,
    bool IsRequired,
    int SortOrder,
    bool IsActive,
    decimal? DisplayDiscountPercent = null);

public sealed record ProductUnitConversionInput(long UnitId, decimal ConversionFactor);

public sealed record ProductEditorInput(
    string? Key,
    string? Name,
    string? EnglishName,
    long BrandId,
    long CategoryId,
    long UnitId,
    decimal PriceAmount,
    decimal DiscountPercent,
    int MaxPurchaseQuantity,
    ProductUnitConversionInput[]? UnitConversions = null);

public sealed record ProductBadgeInput(string? Title);

public sealed record ProductBenefitInput(string? Title, string? Detail, string? Icon, int SortOrder, bool IsActive);

public sealed record ProductCreateInput(
    string? Key,
    string? Name,
    string? EnglishName,
    long BrandId,
    long CategoryId,
    long UnitId,
    decimal PriceAmount,
    decimal DiscountPercent,
    int MaxPurchaseQuantity,
    ProductUnitConversionInput[]? UnitConversions = null);

public sealed record BrandInput(
    string? Key,
    string? NameFa,
    string? NameEn,
    string? CountryCode,
    bool? ShowInStore);

public sealed record CategoryInput(string? Key, string? NameFa, string? NameEn, long? ParentId);

public sealed record AttributeDefinitionInput(
    string? Key,
    string? Label,
    string? Icon,
    string? SourceKey,
    string? FilterKey,
    bool IsFilterable,
    bool IsHighlight);

public sealed record CatalogStatusInput(bool IsActive);
