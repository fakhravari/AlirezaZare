using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Warehouse.Contracts;
using System.Text.Json;

namespace Shop.Admin.Areas.Warehouse.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse/definitions")]
public sealed class WarehouseDefinitionsApiController(IWarehouseDefinitionsAdminData data) : AdminOperationController
{
    [HttpGet("{section}")]
    public async Task<IActionResult> Get(string section, CancellationToken cancellationToken) =>
        Respond(await data.Get(section, cancellationToken));

    [HttpGet("variant-names")]
    public async Task<IActionResult> GetVariantNames(CancellationToken cancellationToken) =>
        Respond(await data.GetVariantNames(cancellationToken));

    [HttpPost("variant-names")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVariantName([FromBody] VariantNameInput input, CancellationToken cancellationToken) =>
        Respond(await data.CreateVariantName(input, cancellationToken));

    [HttpPut("variant-names/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVariantName(long id, [FromBody] VariantNameInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.UpdateVariantName(id, input, cancellationToken));

    [HttpPost("variant-names/{id:long}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateVariantName(long id, CancellationToken cancellationToken) =>
        Respond(await data.ActivateVariantName(id, cancellationToken));

    [HttpDelete("variant-names/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUnusedVariantName(long id, CancellationToken cancellationToken) =>
        Respond(await data.DeleteUnusedVariantName(id, cancellationToken));

    [HttpGet("variant-options/{productId:long}")]
    public async Task<IActionResult> GetVariantOptions(long productId, [FromQuery] long? variantId,
        CancellationToken cancellationToken) =>
        Respond(await data.GetVariantOptions(productId, variantId, cancellationToken));

    [HttpPost("{section}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string section, [FromBody] JsonElement payload,
        CancellationToken cancellationToken) =>
        Respond(await data.Create(section, payload, cancellationToken));

    [HttpPut("{section}/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string section, long id, [FromBody] JsonElement payload,
        CancellationToken cancellationToken) =>
        Respond(await data.Save(section, id, payload, cancellationToken));

    [HttpDelete("{section}/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUnusedUnitDefinition(string section, long id,
        CancellationToken cancellationToken) =>
        Respond(await data.DeleteUnusedUnitDefinition(section, id, cancellationToken));

    [HttpDelete("variants/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUnusedVariant(long id, CancellationToken cancellationToken) =>
        Respond(await data.DeleteUnusedVariant(id, cancellationToken));

    [HttpPut("sales-plan-mapping")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMapping([FromBody] SalesPlanMappingInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.SaveMapping(input, cancellationToken));

    [HttpDelete("sales-plan-mapping/{warehouseId:long}/{ownerId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMapping(long warehouseId, long ownerId, CancellationToken cancellationToken) =>
        Respond(await data.RemoveMapping(warehouseId, ownerId, cancellationToken));

    [HttpPut("sales-plan-mapping/{warehouseId:long}/{ownerId:long}/default")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultMapping(long warehouseId, long ownerId,
        CancellationToken cancellationToken) =>
        Respond(await data.SetDefaultMapping(warehouseId, ownerId, cancellationToken));

    [HttpPost("{section}/{id:long}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string section, long id, CancellationToken cancellationToken) =>
        Respond(await data.Deactivate(section, id, cancellationToken));

    [HttpPost("variants/{id:long}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateVariant(long id, CancellationToken cancellationToken) =>
        Respond(await data.ActivateVariant(id, cancellationToken));
}
