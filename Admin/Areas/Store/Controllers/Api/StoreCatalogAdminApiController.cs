using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/catalog")]
public sealed class StoreCatalogAdminApiController(IStoreCatalogAdminData data) : AdminOperationController
{
    [HttpGet("products/manage/{id:long}/accessories")]
    public async Task<IActionResult> ProductAccessories(long id, CancellationToken cancellationToken) =>
        Respond(await data.ProductAccessories(id, cancellationToken));

    [HttpPost("products/manage/{id:long}/accessories")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProductAccessory(long id, [FromBody] ProductAccessoryInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.CreateProductAccessory(id, input, cancellationToken));

    [HttpPut("products/manage/{id:long}/accessories/{linkId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductAccessory(long id, long linkId,
        [FromBody] ProductAccessoryInput input, CancellationToken cancellationToken) =>
        Respond(await data.UpdateProductAccessory(id, linkId, input, cancellationToken));

    [HttpGet("products/manage/{id:long}")]
    public async Task<IActionResult> ProductEditor(long id, CancellationToken cancellationToken) =>
        Respond(await data.ProductEditor(id, cancellationToken));

    [HttpGet("products/{id:long}/details")]
    public async Task<IActionResult> ProductDetails(long id, CancellationToken cancellationToken) =>
        Respond(await data.ProductDetails(id, cancellationToken));

    [HttpPut("products/manage/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProduct(long id, [FromBody] ProductEditorInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.UpdateProduct(id, input, cancellationToken));

    [HttpPost("product-badges")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProductBadge([FromBody] ProductBadgeInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.CreateProductBadge(input, cancellationToken));

    [HttpGet("products/manage/{id:long}/benefits")]
    public async Task<IActionResult> ProductBenefits(long id, CancellationToken cancellationToken) =>
        Respond(await data.ProductBenefits(id, cancellationToken));

    [HttpPost("products/manage/{id:long}/benefits")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProductBenefit(long id, [FromBody] ProductBenefitInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.CreateProductBenefit(id, input, cancellationToken));

    [HttpPut("products/manage/{id:long}/benefits/{benefitId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductBenefit(long id, long benefitId, [FromBody] ProductBenefitInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.UpdateProductBenefit(id, benefitId, input, cancellationToken));

    [HttpDelete("products/manage/{id:long}/benefits/{benefitId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProductBenefit(long id, long benefitId, CancellationToken cancellationToken) =>
        Respond(await data.DeleteProductBenefit(id, benefitId, cancellationToken));

    [HttpGet("products/drafts")]
    public async Task<IActionResult> ProductDrafts(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await data.ProductDrafts(q, page, pageSize, cancellationToken));

    [HttpGet("products/create-options")]
    public async Task<IActionResult> ProductCreateOptions(CancellationToken cancellationToken) =>
        Respond(await data.ProductCreateOptions(cancellationToken));

    [HttpPost("products/manage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct([FromBody] ProductCreateInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.CreateProduct(input, cancellationToken));

    [HttpPost("products/manage/{id:long}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishProduct(long id, CancellationToken cancellationToken) =>
        Respond(await data.PublishProduct(id, cancellationToken));

    [HttpPost("products/manage/{id:long}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnpublishProduct(long id, CancellationToken cancellationToken) =>
        Respond(await data.UnpublishProduct(id, cancellationToken));

    [HttpGet("brands/countries")]
    public async Task<IActionResult> BrandCountries(CancellationToken cancellationToken) =>
        Respond(await data.BrandCountries(cancellationToken));

    [HttpPost("brands/manage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBrand([FromBody] BrandInput input, CancellationToken cancellationToken) =>
        Respond(await data.CreateBrand(input, cancellationToken));

    [HttpPut("brands/manage/{key}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBrand(string key, [FromBody] BrandInput input, CancellationToken cancellationToken) =>
        Respond(await data.UpdateBrand(key, input, cancellationToken));

    [HttpGet("categories/manage/options")]
    public async Task<IActionResult> CategoryOptions(CancellationToken cancellationToken) =>
        Respond(await data.CategoryOptions(cancellationToken));

    [HttpPost("categories/manage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryInput input, CancellationToken cancellationToken) =>
        Respond(await data.CreateCategory(input, cancellationToken));

    [HttpPut("categories/manage/{key}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(string key, [FromBody] CategoryInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.UpdateCategory(key, input, cancellationToken));

    [HttpGet("attributes/manage/{key}")]
    public async Task<IActionResult> AttributeEditor(string key, CancellationToken cancellationToken) =>
        Respond(await data.AttributeEditor(key, cancellationToken));

    [HttpPost("attributes/manage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAttribute([FromBody] AttributeDefinitionInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.CreateAttribute(input, cancellationToken));

    [HttpPut("attributes/manage/{key}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttribute(string key, [FromBody] AttributeDefinitionInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.UpdateAttribute(key, input, cancellationToken));

    [HttpPost("attributes/manage/{key}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAttributeStatus(string key, [FromBody] CatalogStatusInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.SetAttributeStatus(key, input, cancellationToken));

    [HttpPost("categories/manage/{key}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCategoryStatus(string key, [FromBody] CatalogStatusInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.SetCategoryStatus(key, input, cancellationToken));

    [HttpPost("brands/manage/{key}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBrandStatus(string key, [FromBody] CatalogStatusInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.SetBrandStatus(key, input, cancellationToken));

    [HttpGet("{section}")]
    public async Task<IActionResult> GetSection(
        string section,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await data.GetSection(section, q, page, pageSize, cancellationToken));
}
