using System;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Controllers.Api;
using Shop.Database.Infrastructure.Persistence;
var settings = JsonDocument.Parse(File.ReadAllText("../Shop.Admin/appsettings.Development.json"));
var cs = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
await using var db = new ShopDbContext(new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(cs).Options);
var controller = new WarehouseMovementsApiController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "system.full-access") }, "test")) } } };
var result = await controller.Get(null, null, null, null);
if (result is not OkObjectResult ok) throw new Exception("Unexpected response");
Console.WriteLine(JsonSerializer.Serialize(ok.Value));

