using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreGeographyAdminData(ShopDbContext db) : IStoreGeographyAdminData
{
    public async Task<GeographyAdminData> ReadAsync(CancellationToken ct)
    {
        var countries = await db.GeoCountries.AsNoTracking().OrderBy(x => x.NameFa)
            .Select(x => new GeographyCountryData(x.Id, x.NameFa, x.NameEn, x.CountryCode, x.FlagPath)).ToListAsync(ct);
        var provinces = await db.GeoProvinces.AsNoTracking().Join(db.GeoCountries.AsNoTracking(),
                province => province.IdCountry, country => country.Id, (province, country) => new { province, country })
            .OrderBy(x => x.province.NameFa).Select(x => new GeographyPlaceData(x.province.Id, x.province.NameFa,
                x.province.NameEn, null, x.province.IdCountry, null, x.country.NameFa, null, null,
                x.province.Latitude, x.province.Longitude, null)).ToListAsync(ct);
        var cities = await db.GeoCities.AsNoTracking().Join(db.GeoProvinces.AsNoTracking(),
                city => city.IdProvince, province => province.Id, (city, province) => new { city, province })
            .Join(db.GeoCountries.AsNoTracking(), x => x.city.IdCountry, country => country.Id,
                (x, country) => new { x.city, x.province, country }).OrderBy(x => x.city.NameFa)
            .Select(x => new GeographyPlaceData(x.city.Id, x.city.NameFa, x.city.NameEn, null, x.city.IdCountry,
                x.city.IdProvince, x.country.NameFa + " / " + x.province.NameFa, null, x.city.SourceCountryName,
                x.city.Latitude, x.city.Longitude, x.city.IsProvinceCenter)).ToListAsync(ct);
        return new GeographyAdminData(countries, provinces, cities);
    }

    public async Task<GeographySaveDataResult> SaveAsync(string kind, long? id, GeographyInputData input,
        string fa, string en, string code, string? flag, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (id is null)
            {
                switch (kind)
                {
                    case "countries":
                        var country = new GeoCountry { Id = await db.GeoCountries.Select(x => (long?)x.Id).MaxAsync(ct) + 1 ?? 1,
                            CountryCode = code, NameFa = fa, NameEn = en, FlagPath = flag };
                        db.GeoCountries.Add(country); id = country.Id; break;
                    case "provinces":
                        if (input.CountryId is not long countryId || !await db.GeoCountries.AnyAsync(x => x.Id == countryId, ct))
                            return new(GeographySaveResult.InvalidCountry, id);
                        db.GeoProvinces.Add(new GeoProvince { Id = await db.GeoProvinces.Select(x => (long?)x.Id).MaxAsync(ct) + 1 ?? 1,
                            IdCountry = countryId, NameFa = fa, NameEn = en, Latitude = input.Latitude, Longitude = input.Longitude });
                        break;
                    case "cities":
                        var parent = await db.GeoProvinces.Where(x => x.Id == input.ProvinceId)
                            .Join(db.GeoCountries, x => x.IdCountry, x => x.Id,
                                (province, country) => new { province.Id, province.IdCountry, CountryName = country.NameFa })
                            .SingleOrDefaultAsync(ct);
                        if (parent is null || parent.IdCountry != input.CountryId) return new(GeographySaveResult.InvalidProvince, id);
                        db.GeoCities.Add(new GeoCity { Id = await db.GeoCities.Select(x => (long?)x.Id).MaxAsync(ct) + 1 ?? 1,
                            IdCountry = parent.IdCountry, IdProvince = parent.Id, SourceCountryName = parent.CountryName,
                            NameFa = fa, NameEn = en, Latitude = input.Latitude, Longitude = input.Longitude,
                            IsProvinceCenter = input.IsProvinceCenter ?? false });
                        break;
                }
            }
            else if (kind == "countries")
            {
                var country = await db.GeoCountries.SingleOrDefaultAsync(x => x.Id == id, ct);
                if (country is null) return new(GeographySaveResult.NotFound, id);
                if (country.CountryCode == "IR" && code != "IR") return new(GeographySaveResult.IranCodeFixed, id);
                country.NameFa = fa; country.NameEn = en; country.CountryCode = code;
                if (input.UpdateDetails) country.FlagPath = flag;
            }
            else if (kind == "provinces")
            {
                var province = await db.GeoProvinces.SingleOrDefaultAsync(x => x.Id == id, ct);
                if (province is null) return new(GeographySaveResult.NotFound, id);
                if (input.CountryId is not long countryId || !await db.GeoCountries.AnyAsync(x => x.Id == countryId, ct))
                            return new(GeographySaveResult.InvalidCountry, id);
                if (province.IdCountry != countryId)
                {
                    await db.GeoCities.Where(x => x.IdProvince == province.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.IdCountry, countryId), ct);
                    await db.AccountAddresses.Where(x => x.IdProvince == province.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.IdCountry, countryId), ct);
                    province.IdCountry = countryId;
                }
                province.NameFa = fa; province.NameEn = en;
                if (input.UpdateDetails) { province.Latitude = input.Latitude; province.Longitude = input.Longitude; }
            }
            else
            {
                var city = await db.GeoCities.SingleOrDefaultAsync(x => x.Id == id, ct);
                if (city is null) return new(GeographySaveResult.NotFound, id);
                var parent = await db.GeoProvinces.Where(x => x.Id == input.ProvinceId)
                    .Select(x => new { x.Id, x.IdCountry }).SingleOrDefaultAsync(ct);
                if (parent is null || parent.IdCountry != input.CountryId) return new(GeographySaveResult.InvalidProvince, id);
                if (city.IdCountry != parent.IdCountry || city.IdProvince != parent.Id)
                {
                    await db.AccountAddresses.Where(x => x.IdCity == city.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.IdCountry, parent.IdCountry)
                            .SetProperty(x => x.IdProvince, parent.Id), ct);
                    city.IdCountry = parent.IdCountry; city.IdProvince = parent.Id;
                }
                city.NameFa = fa; city.NameEn = en;
                if (input.UpdateDetails)
                {
                    city.Latitude = input.Latitude; city.Longitude = input.Longitude;
                    city.IsProvinceCenter = input.IsProvinceCenter ?? false;
                }
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new(GeographySaveResult.Saved, id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 or 547 or 515 })
        { return new(GeographySaveResult.Conflict, id); }
    }

    public async Task<GeographyDeleteResult> DeleteAsync(string kind, long id, CancellationToken ct)
    {
        try
        {
            int changed = kind switch
            {
                "countries" => await DeleteOneAsync(db.GeoCountries, id, ct),
                "provinces" => await DeleteOneAsync(db.GeoProvinces, id, ct),
                "cities" => await DeleteOneAsync(db.GeoCities, id, ct),
                _ => -1
            };
            return changed switch { -1 => GeographyDeleteResult.InvalidKind, 0 => GeographyDeleteResult.NotFound,
                _ => GeographyDeleteResult.Deleted };
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        { return GeographyDeleteResult.InUse; }
    }

    private async Task<int> DeleteOneAsync<TEntity>(DbSet<TEntity> set, long id, CancellationToken ct) where TEntity : class
    {
        var item = await set.FindAsync([id], ct);
        if (item is null) return 0;
        set.Remove(item);
        return await db.SaveChangesAsync(ct);
    }
}
