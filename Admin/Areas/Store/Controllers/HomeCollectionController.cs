using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Areas.Store.Controllers;

[Area("Store")]
[Authorize(Roles = "system.full-access")]
public sealed class HomeCollectionController(ShopDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        q = (q ?? "").Trim();
        if (q.Length > 100 || page is < 1 or > 100000) return BadRequest();
        ViewData["Title"] = "محصولات منتخب صفحهٔ اصلی";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "home-collection";
        var collection = await db.StoreProductCollections.AsNoTracking().SingleOrDefaultAsync(x => x.CollectionKey == "selected", cancellationToken);
        long? collectionId = collection?.Id;
        var selected = await (from item in db.StoreProductCollectionItems.AsNoTracking()
                              join product in db.Products.AsNoTracking() on item.IdProduct equals product.Id
                              where item.IdProductCollection == collectionId
                              orderby item.SortOrder, item.Id
                              select new HomeCollectionProduct(item.Id, product.Id, product.Name, product.ProductKey, item.SortOrder, product.IsStoreEnabled)).ToListAsync(cancellationToken);
        var categories = await db.StoreCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.TreePathNames).ThenBy(x => x.Id)
            .Select(x => new HomeCollectionCategory(x.Id, x.TreePathNames)).ToListAsync(cancellationToken);
        bool tabsReady = await db.Database.SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'Store.HomeCategoryTab', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]").SingleAsync(cancellationToken) == 1;
        var tabs = tabsReady ? await (from tab in db.StoreHomeCategoryTabs.AsNoTracking()
                          join category in db.StoreCategories.AsNoTracking() on tab.IdCategory equals category.Id
                          orderby tab.SortOrder, tab.Id
                          select new HomeCollectionTab(tab.Id, category.Id, category.TreePathNames, tab.SortOrder, tab.IsActive, category.IsActive)).ToListAsync(cancellationToken) : [];
        var products = db.Products.AsNoTracking().Where(x => x.IsStoreEnabled && (q == "" || x.Name.Contains(q) || x.ProductKey.Contains(q)));
        int total = await products.CountAsync(cancellationToken);
        var candidates = await products.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * 50).Take(50)
            .Select(x => new HomeCollectionCandidate(x.Id, x.Name, x.ProductKey)).ToListAsync(cancellationToken);
        return View(new HomeCollectionModel(selected, tabs, categories, candidates, q, page, total, tabsReady));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(long productId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        if (!await db.Products.AnyAsync(x => x.Id == productId && x.IsStoreEnabled, cancellationToken)) return BadRequest();
        var collection = await db.StoreProductCollections.SingleOrDefaultAsync(x => x.CollectionKey == "selected", cancellationToken);
        if (collection is null)
        {
            collection = new StoreProductCollection { CollectionKey = "selected", Name = "محصولات منتخب" };
            db.StoreProductCollections.Add(collection);
            await db.SaveChangesAsync(cancellationToken);
        }
        if (!await db.StoreProductCollectionItems.AnyAsync(x => x.IdProductCollection == collection.Id && x.IdProduct == productId, cancellationToken))
        {
            int last = await db.StoreProductCollectionItems.Where(x => x.IdProductCollection == collection.Id).MaxAsync(x => (int?)x.SortOrder, cancellationToken) ?? 0;
            if (last > int.MaxValue - 10) return BadRequest();
            db.StoreProductCollectionItems.Add(new StoreProductCollectionItem { IdProductCollection = collection.Id, IdProduct = productId, SortOrder = last + 10 });
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        TempData["HomeCollectionMessage"] = "فهرست کالاهای منتخب ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Product(long id, int sortOrder, string operation, CancellationToken cancellationToken)
    {
        var item = await (from entry in db.StoreProductCollectionItems join collection in db.StoreProductCollections on entry.IdProductCollection equals collection.Id
                          where entry.Id == id && collection.CollectionKey == "selected" select entry).SingleOrDefaultAsync(cancellationToken);
        if (item is null) return NotFound();
        if (operation == "remove") db.StoreProductCollectionItems.Remove(item);
        else if (operation == "save" && sortOrder >= 0) item.SortOrder = sortOrder;
        else return BadRequest();
        await db.SaveChangesAsync(cancellationToken);
        TempData["HomeCollectionMessage"] = "تغییر کالا ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(long categoryId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        if (!await db.StoreCategories.AnyAsync(x => x.Id == categoryId && x.IsActive, cancellationToken)) return BadRequest();
        if (!await db.StoreHomeCategoryTabs.AnyAsync(x => x.IdCategory == categoryId, cancellationToken))
        {
            int last = await db.StoreHomeCategoryTabs.MaxAsync(x => (int?)x.SortOrder, cancellationToken) ?? 0;
            if (last > int.MaxValue - 10) return BadRequest();
            db.StoreHomeCategoryTabs.Add(new StoreHomeCategoryTab { IdCategory = categoryId, SortOrder = last + 10, IsActive = true });
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        TempData["HomeCollectionMessage"] = "فهرست دسته‌ها ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Category(long id, int sortOrder, bool isActive, string operation, CancellationToken cancellationToken)
    {
        var tab = await db.StoreHomeCategoryTabs.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tab is null) return NotFound();
        if (operation == "remove") db.StoreHomeCategoryTabs.Remove(tab);
        else if (operation == "save" && sortOrder >= 0) { tab.SortOrder = sortOrder; tab.IsActive = isActive; }
        else return BadRequest();
        await db.SaveChangesAsync(cancellationToken);
        TempData["HomeCollectionMessage"] = "تغییر دسته ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }
}

public sealed record HomeCollectionProduct(long Id, long ProductId, string Name, string Code, int SortOrder, bool IsStoreEnabled);
public sealed record HomeCollectionCategory(long Id, string Name);
public sealed record HomeCollectionTab(long Id, long CategoryId, string Name, int SortOrder, bool IsActive, bool CategoryActive);
public sealed record HomeCollectionCandidate(long Id, string Name, string Code);
public sealed record HomeCollectionModel(List<HomeCollectionProduct> Products, List<HomeCollectionTab> Tabs, List<HomeCollectionCategory> Categories, List<HomeCollectionCandidate> Candidates, string Query, int Page, int Total, bool TabsReady);
