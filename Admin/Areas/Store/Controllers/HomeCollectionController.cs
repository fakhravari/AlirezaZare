using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers;

[Area("Store")]
[Authorize(Roles = "system.full-access")]
public sealed class HomeCollectionController(IStoreHomeCollectionAdminData data) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "محصولات منتخب صفحهٔ اصلی";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "home-collection";
        var result = await data.ReadAsync(cancellationToken);
        return View(new HomeCollectionModel(
            result.Products.Select(x => new HomeCollectionProduct(x.Id, x.ProductId, x.Name, x.Code, x.SortOrder,
                x.IsStoreEnabled)).ToList(),
            result.Tabs.Select(x => new HomeCollectionTab(x.Id, x.CategoryId, x.Name, x.SortOrder, x.IsActive,
                x.CategoryActive)).ToList(),
            result.Categories.Select(x => new HomeCollectionCategory(x.Id, x.Name)).ToList(),
            result.TabsReady));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProducts(decimal[]? productIds, CancellationToken cancellationToken)
    {
        if (productIds is null || productIds.Length is < 1 or > 100) return BadRequest();
        var result = await data.AddProductsAsync(productIds, cancellationToken);
        if (result != HomeCollectionMutationResult.Saved) return BadRequest();
        TempData["HomeCollectionMessage"] = "فهرست کالاهای منتخب ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Candidates(string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        string search = (q ?? "").Trim();
        if (search.Length > 100 || page is < 1 or > 10000) return BadRequest();
        var result = await data.ReadCandidatesAsync(search, page, cancellationToken);
        return Json(new
        {
            items = result.Items.Select(item => new { id = item.Id, name = item.Name, code = item.Code }),
            more = result.More
        });
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
    public async Task<IActionResult> AddCategories(long[]? categoryIds, CancellationToken cancellationToken)
    {
        if (categoryIds is null || categoryIds.Length is < 1 or > 50) return BadRequest();
        var result = await data.AddCategoriesAsync(categoryIds, cancellationToken);
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

public sealed record HomeCollectionModel(
    List<HomeCollectionProduct> Products,
    List<HomeCollectionTab> Tabs,
    List<HomeCollectionCategory> Categories,
    bool TabsReady);
