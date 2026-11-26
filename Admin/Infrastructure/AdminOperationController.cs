using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Admin.Contracts;

namespace Shop.Admin.Infrastructure;

public abstract class AdminOperationController : ControllerBase
{
    protected IActionResult Respond(AdminOperationResult result) => result.Status switch
    {
        AdminOperationStatus.Success => Ok(result.Body),
        AdminOperationStatus.Invalid => result.Body is null ? BadRequest() : BadRequest(result.Body),
        AdminOperationStatus.Missing => result.Body is null ? NotFound() : NotFound(result.Body),
        AdminOperationStatus.Denied => Forbid(),
        AdminOperationStatus.Conflict => Conflict(result.Body),
        AdminOperationStatus.Validation => ValidationProblem(detail: result.Detail, statusCode: 400),
        _ => throw new InvalidOperationException()
    };
}
