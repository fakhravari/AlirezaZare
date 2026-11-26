namespace Shop.Data.Application.Store.Contracts;

public interface IStoreCatalogFeaturesAdminData
{
    Task<IReadOnlyList<CatalogFeatureData>> ReadFeaturesAsync(CancellationToken cancellationToken);
    Task<CategoryFeatureData?> ReadCategoryFeaturesAsync(string key, CancellationToken cancellationToken);
    Task<ProductFeatureData> ReadProductFeaturesAsync(long productId, CancellationToken cancellationToken);
    Task<CatalogFeatureMutationResult> CreateFeatureAsync(string name, byte kind,
        CancellationToken cancellationToken);
    Task<CatalogFeatureValueMutationResult> CreateValueAsync(long featureId, string name,
        CancellationToken cancellationToken);
    Task<CatalogFeatureRenameStatus> RenameFeatureAsync(long featureId, string name,
        CancellationToken cancellationToken);
    Task<CatalogFeatureRenameStatus> RenameValueAsync(long featureId, long valueId, string name,
        CancellationToken cancellationToken);
    Task<SaveCategoryFeaturesStatus> SaveCategoryFeaturesAsync(string key, IReadOnlyList<long> featureIds,
        CancellationToken cancellationToken);
    Task<SaveProductFeaturesStatus> SaveProductFeaturesAsync(long productId,
        IReadOnlyList<ProductFeatureSelectionData> selections, CancellationToken cancellationToken);
}

public sealed record CatalogFeatureData(long Id, string Name, byte Kind, bool IsActive,
    IReadOnlyList<CatalogFeatureValueData> Values);

public sealed record CatalogFeatureValueData(long Id, string Name, bool IsActive);
public sealed record CategoryFeatureData(bool ValidHierarchy, IReadOnlyList<long> Direct,
    IReadOnlyList<long> Inherited);
public sealed record ProductFeatureData(bool Exists, bool ValidHierarchy, IReadOnlyList<long> FeatureIds,
    IReadOnlyList<ProductFeatureSelectionData> Selections);
public sealed record ProductFeatureSelectionData(long FeatureId, long ValueId);
public enum CatalogFeatureMutationStatus { Created, Duplicate }
public sealed record CatalogFeatureMutationResult(CatalogFeatureMutationStatus Status, long Id, string Name, byte Kind);
public enum CatalogFeatureValueMutationStatus { Created, NotFound, TooLong, Duplicate }
public sealed record CatalogFeatureValueMutationResult(CatalogFeatureValueMutationStatus Status, long Id, string Name);
public enum CatalogFeatureRenameStatus { Saved, NotFound, InvalidName, Duplicate }
public enum SaveCategoryFeaturesStatus { Saved, NotFound, InvalidHierarchy, InvalidFeatures, InUse }
public enum SaveProductFeaturesStatus { Saved, NotFound, InvalidHierarchy, InvalidSelections }
