using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreCustomersAdminData(ShopDbContext db) : IStoreCustomersAdminData
{
    public async Task<CustomerAdminPage> ReadAsync(string search, int offset, int pageSize, CancellationToken ct)
    {
        var query = db.AccountProfiles.AsNoTracking().Where(p => search == "" ||
            (p.FirstName + " " + p.LastName).Contains(search) || p.Email.Contains(search) || (p.Phone ?? "").Contains(search));
        int total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(p => p.Id).Skip(offset).Take(pageSize).Select(p =>
            new CustomerAdminRow(p.Id, p.FirstName, p.LastName, p.Email, p.Phone, p.CreatedAtUtc,
                db.StoreCustomerOrders.Count(o => o.IdProfile == p.Id))).ToListAsync(ct);
        return new CustomerAdminPage(rows, total);
    }

    public async Task<CustomerAdminDetails?> ReadDetailsAsync(decimal id, CancellationToken ct)
    {
        var baseRow = await db.AccountProfiles.AsNoTracking().Where(p => p.Id == id).Select(p =>
            new CustomerAdminRow(p.Id, p.FirstName, p.LastName, p.Email, p.Phone, p.CreatedAtUtc,
                db.StoreCustomerOrders.Count(o => o.IdProfile == p.Id))).SingleOrDefaultAsync(ct);
        if (baseRow is null) return null;
        int favorites = await db.AccountFavorites.CountAsync(x => x.IdProfile == id, ct);
        int alerts = await db.AccountPriceAlerts.CountAsync(x => x.IdProfile == id, ct);
        int articleInteractions = await db.AccountArticleInteractions.CountAsync(x => x.IdProfile == id, ct);
        var addresses = await db.AccountAddresses.AsNoTracking().Where(x => x.IdProfile == id)
            .Join(db.GeoCountries.AsNoTracking(), x => x.IdCountry, x => x.Id,
                (address, country) => new { address, country })
            .Join(db.GeoProvinces.AsNoTracking(), x => x.address.IdProvince, x => x.Id,
                (x, province) => new { x.address, x.country, province })
            .Join(db.GeoCities.AsNoTracking(), x => x.address.IdCity, x => x.Id,
                (x, city) => new { x.address, x.country, x.province, city }).OrderBy(x => x.address.Id)
            .Select(x => new { x.address.Title, x.address.Recipient, x.address.Phone, Country = x.country.NameFa,
                Province = x.province.NameFa, City = x.city.NameFa, x.address.Street, x.address.PostalCode,
                x.address.IsDefault }).ToListAsync(ct);
        var orders = await db.StoreCustomerOrders.AsNoTracking().Where(x => x.IdProfile == id).OrderByDescending(x => x.Id)
            .Take(50).Select(x => new { x.OrderNumber, Status = x.StatusReference.NameFa, x.TotalAmount, x.CreatedAtUtc })
            .ToListAsync(ct);
        var feedback = await db.ProductFeedback.AsNoTracking().Where(x => x.IdProfile == id).OrderByDescending(x => x.Id)
            .Take(50).Select(x => new { x.FeedbackType, x.FeedbackText, Status = x.Status.Name, x.CreatedAtUtc,
                ProductName = x.Product.NameFa }).ToListAsync(ct);
        var cartItems = await db.AccountCarts.AsNoTracking().Join(db.AccountCartItems.AsNoTracking(),
                cart => cart.Id, item => item.IdCart, (cart, item) => new { cart, item })
            .Join(db.Products.AsNoTracking(), x => x.item.IdProduct, p => p.Id, (x, product) => new { x.cart, x.item, product })
            .Join(db.SalesPlanReferences.AsNoTracking(), x => x.item.IdSalesPlan, plan => plan.Id,
                (x, plan) => new { x.cart, x.item, x.product, plan }).Where(x => x.cart.IdProfile == id)
            .OrderBy(x => x.item.Id).Select(x => new { x.item.Id, ProductName = x.product.NameFa,
                IdProductVariant = x.item.IdVariant, VariantPath = db.ProductVariants.Where(v => v.Id == x.item.IdVariant)
                    .Select(v => v.VariantName.TreePathNames).FirstOrDefault(), SalesPlanName = x.plan.DisplayName,
                x.item.IdSalesPlan, x.item.Quantity }).ToListAsync(ct);
        decimal[] cartIds = cartItems.Select(x => x.Id).ToArray();
        var accessories = cartIds.Length == 0 ? [] : await db.AccountCartItemAccessories.AsNoTracking()
            .Join(db.StoreProductAccessories.AsNoTracking(), x => x.IdProductAccessory, y => y.Id,
                (selected, link) => new { selected, link })
            .Join(db.StoreAccessories.AsNoTracking(), x => x.link.IdAccessory, y => y.Id,
                (x, accessory) => new { x.selected, x.link, accessory })
            .Where(x => cartIds.Contains(x.selected.IdCartItem)).OrderBy(x => x.selected.IdCartItem)
            .ThenBy(x => x.link.SortOrder).ThenBy(x => x.link.Id)
            .Select(x => new { x.selected.IdCartItem, x.accessory.Name, x.link.IsRequired }).ToListAsync(ct);
        string cartJson = JsonSerializer.Serialize(cartItems.Select(item => new { item.ProductName, item.VariantPath,
            item.IdSalesPlan, item.Quantity, Accessories = accessories.Where(x => x.IdCartItem == item.Id)
                .Select(x => new { x.Name, x.IsRequired }) }));
        return new CustomerAdminDetails(baseRow, JsonSerializer.Serialize(addresses), JsonSerializer.Serialize(orders),
            JsonSerializer.Serialize(feedback), cartJson, favorites, alerts, articleInteractions);
    }

    public async Task<CustomerCreateDataResult> CreateAsync(string email, string firstName, string lastName, string? phone,
        CancellationToken ct)
    {
        if (await db.AccountProfiles.AnyAsync(x => x.Email == email, ct))
            return new(CustomerMutationResult.EmailExists, null);
        var profile = new AccountProfile { Email = email, FirstName = firstName, LastName = lastName,
            Phone = phone, CreatedAtUtc = DateTime.UtcNow };
        db.AccountProfiles.Add(profile);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { return new(CustomerMutationResult.EmailExists, null); }
        return new(CustomerMutationResult.Saved, profile.Id);
    }

    public async Task<CustomerMutationResult> UpdateAsync(long id, CustomerAdminUpdate input, CancellationToken ct)
    {
        if (await db.AccountProfiles.AnyAsync(x => x.Id != id && x.Email == input.Email, ct))
            return CustomerMutationResult.EmailExists;
        try
        {
            int changed = await db.AccountProfiles.Where(x => x.Id == id && x.Email == input.ExpectedEmail &&
                x.FirstName == input.ExpectedFirstName && x.LastName == input.ExpectedLastName &&
                x.Phone == input.ExpectedPhone).ExecuteUpdateAsync(update => update.SetProperty(x => x.Email, input.Email)
                    .SetProperty(x => x.FirstName, input.FirstName).SetProperty(x => x.LastName, input.LastName)
                    .SetProperty(x => x.Phone, input.Phone).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);
            return changed == 0 ? CustomerMutationResult.Changed : CustomerMutationResult.Saved;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { return CustomerMutationResult.EmailExists; }
    }

    public async Task<CustomerMutationResult> DeleteAsync(long id, CancellationToken ct)
    {
        if (!await db.AccountProfiles.AnyAsync(x => x.Id == id, ct)) return CustomerMutationResult.NotFound;
        bool hasOrders = await db.Set<StoreCustomerOrderReference>().AnyAsync(x => x.IdProfile == id, ct);
        bool hasCart = await db.AccountCarts.AnyAsync(x => x.IdProfile == id, ct);
        if (hasOrders || hasCart) return CustomerMutationResult.HasOrdersOrCart;
        await db.AccountProfiles.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
        return CustomerMutationResult.Deleted;
    }
}
