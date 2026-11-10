using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Controllers.Api;

[ApiController]
[Route("api/store/geography")]
public sealed class GeographyApiController(IStoreRuntimeDatasetRepository datasets) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPlaces(CancellationToken cancellationToken) =>
        Content(await datasets.ReadGeographyJsonAsync(cancellationToken), "application/json; charset=utf-8");
}