using System.Data;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreHomeCollectionAdminData(ShopDbContext db) : IStoreHomeCollectionAdminData
{
    public async Task<HomeCollectionAdminData> ReadAsync(string search, int page,
        CancellationToken cancellationToken)
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
        var products = db.Products.AsNoTracking().Where(item => item.IsStoreEnabled &&
            (search == "" || item.NameFa.Contains(search) || item.ProductKey.Contains(search)));
        int total = await products.CountAsync(cancellationToken);
        var candidates = await products.OrderBy(item => item.NameFa).ThenBy(item => item.Id)
            .Skip((page - 1) * 50).Take(50)
            .Select(item => new HomeCollectionCandidateData(item.Id, item.NameFa, item.ProductKey))
            .ToListAsync(cancellationToken);
        return new HomeCollectionAdminData(selected, tabs, categories, candidates, search, page, total, tabsReady);
    }

    public async Task<HomeCollectionMutationResult> AddProductAsync(decimal productId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        if (!await db.Products.AnyAsync(item => item.Id == productId && item.IsStoreEnabled, cancellationToken))
            return HomeCollectionMutationResult.InvalidProduct;
        var collection = await db.StoreProductCollections.SingleOrDefaultAsync(item =>
            item.CollectionKey == "selected", cancellationToken);
        if (collection is null)
        {
            collection = new StoreProductCollection { CollectionKey = "selected", Name = "محصولات منتخب" };
            db.StoreProductCollections.Add(collection);
            await db.SaveChangesAsync(cancellationToken);
        }
        if (!await db.StoreProductCollectionItems.AnyAsync(item => item.IdCollection == collection.Id &&
                item.IdProduct == productId, cancellationToken))
        {
            int last = await db.StoreProductCollectionItems.Where(item => item.IdCollection == collection.Id)
                .MaxAsync(item => (int?)item.SortOrder, cancellationToken) ?? 0;
            if (last > int.MaxValue - 10) return HomeCollectionMutationResult.SortOrderLimit;
            db.StoreProductCollectionItems.Add(new StoreProductCollectionItem
                { IdCollection = collection.Id, IdProduct = productId, SortOrder = last + 10 });
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

    public async Task<HomeCollectionMutationResult> AddCategoryAsync(long categoryId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        if (!await db.StoreCategories.AnyAsync(item => item.Id == categoryId && item.IsActive, cancellationToken))
            return HomeCollectionMutationResult.InvalidCategory;
        if (!await db.StoreHomeCategoryTabs.AnyAsync(item => item.IdCategory == categoryId, cancellationToken))
        {
            int last = await db.StoreHomeCategoryTabs.MaxAsync(item => (int?)item.SortOrder, cancellationToken) ?? 0;
            if (last > int.MaxValue - 10) return HomeCollectionMutationResult.SortOrderLimit;
            db.StoreHomeCategoryTabs.Add(new StoreHomeCategoryTab
                { IdCategory = categoryId, SortOrder = last + 10, IsActive = true });
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
