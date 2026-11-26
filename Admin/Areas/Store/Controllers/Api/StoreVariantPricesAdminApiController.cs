using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/variant-prices")]
public sealed class StoreVariantPricesAdminApiController(IStoreVariantPricesAdminData data) : AdminOperationController
{
    [HttpGet("plans")]
    public async Task<IActionResult> Plans([FromQuery] string? q, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        CancellationToken cancellationToken) =>
        Respond(await data.Plans(q, from, to, cancellationToken));

    [HttpGet("price-plans")]
    public async Task<IActionResult> PricePlans(CancellationToken cancellationToken) =>
        Respond(await data.PricePlans(cancellationToken));

    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] string? q, [FromQuery] int page = 1,
        CancellationToken cancellationToken = default) =>
        Respond(await data.Products(q, page, cancellationToken));

    [HttpGet("price-history")]
    public async Task<IActionResult> PriceHistory([FromQuery] decimal? productId, [FromQuery] decimal[]? productIds,
        [FromQuery] long? salesPlanId, [FromQuery] string? q, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int take = 100, CancellationToken cancellationToken = default) =>
        Respond(await data.PriceHistory(productId, productIds, salesPlanId, q, from, to, take, cancellationToken));

    [HttpPost("plans")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlan([FromBody] PlanInput input, CancellationToken cancellationToken) =>
        Respond(await data.CreatePlan(input, cancellationToken));

    [HttpPut("plans/{id:long}/details")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePlan(long id, [FromBody] PlanInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.UpdatePlan(id, input, cancellationToken));

    [HttpPut("plans/{id:long}/publication")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPlanPublication(long id, [FromBody] PlanPublicationInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.SetPlanPublication(id, input, cancellationToken));

    [HttpDelete("plans/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlan(long id, CancellationToken cancellationToken) =>
        Respond(await data.DeletePlan(id, cancellationToken));

    [HttpPut("plans/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePlan(long id, [FromBody] PlanSettingsInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.SavePlan(id, input, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] decimal[]? productIds,
        [FromQuery] long? salesPlanId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] bool all = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await data.List(q, productIds, salesPlanId, from, to, all, page, pageSize, cancellationToken));

    [HttpPut("{variantId:long}/{salesPlanId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(long variantId, string salesPlanId, [FromBody] VariantPlanPriceInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.Save(variantId, salesPlanId, input, cancellationToken));

    [HttpGet("product-offers")]
    public async Task<IActionResult> ProductOffers(
        [FromQuery] string? q,
        [FromQuery] decimal[]? productIds,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await data.ProductOffers(q, productIds, from, to, page, pageSize, cancellationToken));

    [HttpPut("product-offers/{productId:long}/{salesPlanId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProductOffer(long productId, string salesPlanId,
        [FromBody] ProductOfferInput input, CancellationToken cancellationToken) =>
        Respond(await data.SaveProductOffer(productId, salesPlanId, input, cancellationToken));

    [HttpGet("product-offers/{productId:long}/{salesPlanId}/accessories")]
    public async Task<IActionResult> OfferAccessories(decimal productId, string salesPlanId,
        CancellationToken cancellationToken) =>
        Respond(await data.OfferAccessories(productId, salesPlanId, cancellationToken));

    [HttpPut("product-offers/{productId:long}/{salesPlanId}/accessories/{accessoryId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOfferAccessory(decimal productId, string salesPlanId, decimal accessoryId,
        [FromBody] OfferAccessoryInput input, CancellationToken cancellationToken) =>
        Respond(await data.SaveOfferAccessory(productId, salesPlanId, accessoryId, input, cancellationToken));

    [HttpGet("product-offers/{productId:long}/{salesPlanId}/specifications")]
    public async Task<IActionResult> OfferSpecifications(decimal productId, string salesPlanId,
        CancellationToken cancellationToken) =>
        Respond(await data.OfferSpecifications(productId, salesPlanId, cancellationToken));

    [HttpPut("product-offers/{productId:long}/{salesPlanId}/specifications")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOfferSpecifications(decimal productId, string salesPlanId,
        [FromBody] List<OfferSpecification> input, CancellationToken cancellationToken) =>
        Respond(await data.SaveOfferSpecifications(productId, salesPlanId, input, cancellationToken));
}
