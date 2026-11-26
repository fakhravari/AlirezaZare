namespace Shop.Data.Application.Store.Contracts;

public interface IStoreGeographyAdminData
{
    Task<GeographyAdminData> ReadAsync(CancellationToken cancellationToken);
    Task<GeographySaveDataResult> SaveAsync(string kind, long? id, GeographyInputData input, string fa, string en,
        string code, string? flag, CancellationToken cancellationToken);
    Task<GeographyDeleteResult> DeleteAsync(string kind, long id, CancellationToken cancellationToken);
}

public sealed record GeographyCountryData(long Id, string NameFa, string NameEn, string Code, string? FlagPath);
public sealed record GeographyPlaceData(long Id, string NameFa, string NameEn, string? Code, long? CountryId,
    long? ProvinceId, string? ParentName, string? FlagPath, string? SourceCountryName, decimal? Latitude,
    decimal? Longitude, bool? IsProvinceCenter);
public sealed record GeographyAdminData(IReadOnlyList<GeographyCountryData> Countries,
    IReadOnlyList<GeographyPlaceData> Provinces, IReadOnlyList<GeographyPlaceData> Cities);
public sealed record GeographyInputData(long? CountryId, long? ProvinceId, decimal? Latitude, decimal? Longitude,
    bool? IsProvinceCenter, bool UpdateDetails);
public enum GeographySaveResult { Saved, NotFound, InvalidCountry, InvalidProvince, IranCodeFixed, Conflict }
public sealed record GeographySaveDataResult(GeographySaveResult Result, long? Id);
public enum GeographyDeleteResult { Deleted, NotFound, InvalidKind, InUse }
