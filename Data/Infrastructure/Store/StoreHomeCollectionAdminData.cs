using System.Data;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreHomeCollectionAdminData(ShopDbContext db) : IStoreHomeCollectionAdminData
{
    public async Task<HomeCollectionAdminData> ReadAsync(CancellationToken cancellationToken)
    {
        var collection = await db.StoreProductCollections.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CollectionKey == "selected", cancellationToken);
        var selected = collection is null
            ? []
            : await db.StoreProductCollectionItems.AsNoTracking()
                .Join(db.Products.AsNoTracking(), item => item.IdProduct, product => product.Id,
                    (item, product) => new { item, product })
                .Where(row => row.item.IdCollection == collection.Id)
                .OrderBy(row => row.item.SortOrder).ThenBy(row => row.item.Id)
                .Select(row => new HomeCollectionProductData(row.item.Id, row.product.Id, row.product.NameFa,
                    row.product.ProductKey, row.item.SortOrder, row.product.IsStoreEnabled))
                .ToListAsync(cancellationToken);
        var categories = await db.StoreCategories.AsNoTracking().Where(item => item.IsActive)
            .OrderBy(item => item.TreePathNames).ThenBy(item => item.Id)
            .Select(item => new HomeCollectionCategoryData(item.Id, item.TreePathNames)).ToListAsync(cancellationToken);
        bool tabsReady = await db.Database.SqlQueryRaw<int>(
            "SELECT CASE WHEN OBJECT_ID(N'Store.HomeCategoryTab', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
            .SingleAsync(cancellationToken) == 1;
        var tabs = tabsReady
            ? await db.StoreHomeCategoryTabs.AsNoTracking()
                .Join(db.StoreCategories.AsNoTracking(), tab => tab.IdCategory, category => category.Id,
                    (tab, category) => new { tab, category })
                .OrderBy(row => row.tab.SortOrder).ThenBy(row => row.tab.Id)
                .Select(row => new HomeCollectionTabData(row.tab.Id, row.category.Id, row.category.TreePathNames,
                    row.tab.SortOrder, row.tab.IsActive, row.category.IsActive)).ToListAsync(cancellationToken)
            : [];
        return new HomeCollectionAdminData(selected, tabs, categories, tabsReady);
    }

    public async Task<HomeCollectionCandidatesPage> ReadCandidatesAsync(string search, int page,
        CancellationToken cancellationToken)
    {
        var selectedProductIds = db.StoreProductCollectionItems.AsNoTracking()
            .Where(item => db.StoreProductCollections.Any(collection =>
                collection.Id == item.IdCollection && collection.CollectionKey == "selected"))
            .Select(item => item.IdProduct);
        var products = db.Products.AsNoTracking().Where(item => item.IsStoreEnabled &&
            !selectedProductIds.Contains(item.Id) &&
            (search == "" || item.NameFa.Contains(search) || item.ProductKey.Contains(search)));
        const int pageSize = 30;
        var rows = await products.OrderBy(item => item.NameFa).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize + 1)
            .Select(item => new HomeCollectionCandidateData(item.Id, item.NameFa, item.ProductKey))
            .ToListAsync(cancellationToken);
        return new HomeCollectionCandidatesPage(rows.Take(pageSize).ToArray(), rows.Count > pageSize);
    }

    public async Task<HomeCollectionMutationResult> AddProductsAsync(IReadOnlyCollection<decimal> productIds,
        CancellationToken cancellationToken)
    {
        decimal[] ids = productIds.Distinct().ToArray();
        if (ids.Length == 0 || ids.Length > 100) return HomeCollectionMutationResult.InvalidProduct;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        int availableCount = await db.Products.CountAsync(item => ids.Contains(item.Id) && item.IsStoreEnabled,
            cancellationToken);
        if (availableCount != ids.Length)
            return HomeCollectionMutationResult.InvalidProduct;

        var collection = await db.StoreProductCollections.SingleOrDefaultAsync(item =>
            item.CollectionKey == "selected", cancellationToken);
        if (collection is null)
        {
            collection = new StoreProductCollection { CollectionKey = "selected", Name = "محصولات منتخب" };
            db.StoreProductCollections.Add(collection);
            await db.SaveChangesAsync(cancellationToken);
        }
        var existing = await db.StoreProductCollectionItems.Where(item => item.IdCollection == collection.Id)
            .Select(item => item.IdProduct).ToListAsync(cancellationToken);
        decimal[] toAdd = ids.Except(existing).ToArray();
        if (toAdd.Length > 0)
        {
            int last = await db.StoreProductCollectionItems.Where(item => item.IdCollection == collection.Id)
                .MaxAsync(item => (int?)item.SortOrder, cancellationToken) ?? 0;
            if ((long)last + toAdd.Length * 10L > int.MaxValue)
                return HomeCollectionMutationResult.SortOrderLimit;
            db.StoreProductCollectionItems.AddRange(toAdd.Select((id, index) => new StoreProductCollectionItem
                { IdCollection = collection.Id, IdProduct = id, SortOrder = last + (index + 1) * 10 }));
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return HomeCollectionMutationResult.Saved;
    }

    public async Task<HomeCollectionMutationResult> UpdateProductAsync(long id, int sortOrder, string operation,
        CancellationToken cancellationToken)
    {
        var item = await db.StoreProductCollectionItems.Join(db.StoreProductCollections,
                entry => entry.IdCollection, collection => collection.Id, (entry, collection) => new { entry, collection })
            .Where(row => row.entry.Id == id && row.collection.CollectionKey == "selected")
            .Select(row => row.entry).SingleOrDefaultAsync(cancellationToken);
        if (item is null) return HomeCollectionMutationResult.NotFound;
        if (operation == "remove") db.StoreProductCollectionItems.Remove(item);
        else if (operation == "save" && sortOrder >= 0) item.SortOrder = sortOrder;
        else return HomeCollectionMutationResult.InvalidOperation;
        await db.SaveChangesAsync(cancellationToken);
        return HomeCollectionMutationResult.Saved;
    }

    public async Task<HomeCollectionMutationResult> AddCategoriesAsync(IReadOnlyCollection<long> categoryIds,
        CancellationToken cancellationToken)
    {
        long[] ids = categoryIds.Distinct().ToArray();
        if (ids.Length == 0 || ids.Length > 50) return HomeCollectionMutationResult.InvalidCategory;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        int activeCount = await db.StoreCategories.CountAsync(item => ids.Contains(item.Id) && item.IsActive,
            cancellationToken);
        if (activeCount != ids.Length)
            return HomeCollectionMutationResult.InvalidCategory;

        var existing = await db.StoreHomeCategoryTabs.Where(item => ids.Contains(item.IdCategory))
            .Select(item => item.IdCategory).ToListAsync(cancellationToken);
        long[] toAdd = ids.Except(existing).ToArray();
        if (toAdd.Length > 0)
        {
            int last = await db.StoreHomeCategoryTabs.MaxAsync(item => (int?)item.SortOrder, cancellationToken) ?? 0;
            if ((long)last + toAdd.Length * 10L > int.MaxValue)
                return HomeCollectionMutationResult.SortOrderLimit;
            db.StoreHomeCategoryTabs.AddRange(toAdd.Select((id, index) => new StoreHomeCategoryTab
                { IdCategory = id, SortOrder = last + (index + 1) * 10, IsActive = true }));
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return HomeCollectionMutationResult.Saved;
    }

    public async Task<HomeCollectionMutationResult> UpdateCategoryAsync(long id, int sortOrder, bool isActive,
        string operation, CancellationToken cancellationToken)
    {
        var tab = await db.StoreHomeCategoryTabs.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (tab is null) return HomeCollectionMutationResult.NotFound;
        if (operation == "remove") db.StoreHomeCategoryTabs.Remove(tab);
        else if (operation == "save" && sortOrder >= 0)
        {
            tab.SortOrder = sortOrder;
            tab.IsActive = isActive;
        }
        else return HomeCollectionMutationResult.InvalidOperation;
        await db.SaveChangesAsync(cancellationToken);
        return HomeCollectionMutationResult.Saved;
    }
}
