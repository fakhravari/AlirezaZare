using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers;

[Area("Store")]
[Authorize(Roles = "system.full-access")]
public sealed class HomeCollectionController(IStoreHomeCollectionAdminData data) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        q = (q ?? "").Trim();
        if (q.Length > 100 || page is < 1 or > 100000) return BadRequest();
        ViewData["Title"] = "محصولات منتخب صفحهٔ اصلی";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "home-collection";
        var result = await data.ReadAsync(q, page, cancellationToken);
        return View(new HomeCollectionModel(
            result.Products.Select(x => new HomeCollectionProduct(x.Id, x.ProductId, x.Name, x.Code, x.SortOrder,
                x.IsStoreEnabled)).ToList(),
            result.Tabs.Select(x => new HomeCollectionTab(x.Id, x.CategoryId, x.Name, x.SortOrder, x.IsActive,
                x.CategoryActive)).ToList(),
            result.Categories.Select(x => new HomeCollectionCategory(x.Id, x.Name)).ToList(),
            result.Candidates.Select(x => new HomeCollectionCandidate(x.Id, x.Name, x.Code)).ToList(), q, page,
            result.Total, result.TabsReady));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(decimal productId, CancellationToken cancellationToken)
    {
        var result = await data.AddProductAsync(productId, cancellationToken);
        if (result != HomeCollectionMutationResult.Saved) return BadRequest();
        TempData["HomeCollectionMessage"] = "فهرست کالاهای منتخب ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Product(long id, int sortOrder, string operation,
        CancellationToken cancellationToken)
    {
        var result = await data.UpdateProductAsync(id, sortOrder, operation, cancellationToken);
        if (result == HomeCollectionMutationResult.NotFound) return NotFound();
        if (result != HomeCollectionMutationResult.Saved) return BadRequest();
        TempData["HomeCollectionMessage"] = "تغییر کالا ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(long categoryId, CancellationToken cancellationToken)
    {
        var result = await data.AddCategoryAsync(categoryId, cancellationToken);
        if (result != HomeCollectionMutationResult.Saved) return BadRequest();
        TempData["HomeCollectionMessage"] = "فهرست دسته‌ها ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Category(long id, int sortOrder, bool isActive, string operation,
        CancellationToken cancellationToken)
    {
        var result = await data.UpdateCategoryAsync(id, sortOrder, isActive, operation, cancellationToken);
        if (result == HomeCollectionMutationResult.NotFound) return NotFound();
        if (result != HomeCollectionMutationResult.Saved) return BadRequest();
        TempData["HomeCollectionMessage"] = "تغییر دسته ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }
}

public sealed record HomeCollectionProduct(
    decimal Id,
    decimal ProductId,
    string Name,
    string Code,
    int SortOrder,
    bool IsStoreEnabled);

public sealed record HomeCollectionCategory(long Id, string Name);

public sealed record HomeCollectionTab(
    long Id,
    long CategoryId,
    string Name,
    int SortOrder,
    bool IsActive,
    bool CategoryActive);

public sealed record HomeCollectionCandidate(decimal Id, string Name, string Code);

public sealed record HomeCollectionModel(
    List<HomeCollectionProduct> Products,
    List<HomeCollectionTab> Tabs,
    List<HomeCollectionCategory> Categories,
    List<HomeCollectionCandidate> Candidates,
    string Query,
    int Page,
    int Total,
    bool TabsReady);
