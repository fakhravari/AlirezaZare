using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class WarrantyController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("ضمانت اصالت | ندا اولیائی", "ضمانت اصالت؛ اطمینان، با جزئیات روشن"));
        return await PageAsync(model);
    }
}
