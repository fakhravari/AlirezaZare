using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public abstract class PageController : Controller
{
    protected async Task<ViewResult> PageAsync(PageModel model)
    {
        ViewResult result = string.IsNullOrWhiteSpace(model.ViewName)? View(model) : View(model.ViewName, model);
        return await Task.FromResult(result);
    }
}
