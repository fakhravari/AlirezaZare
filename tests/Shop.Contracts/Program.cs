using System.Text.Json.Nodes;
using Shop.Services;

string sourcePath = args.Length > 0 ? Path.GetFullPath(args[0]) :
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Shop/wwwroot/content/data/catalog.json"));
string source = File.ReadAllText(sourcePath);
CatalogSnapshot snapshot = CatalogSnapshot.Parse(source);
if (snapshot.Products.Count != 30 || snapshot.Brands.Count != 21)
    throw new Exception("Catalog extraction changed product/brand identity.");
if (snapshot.Products.Single(item => item.Id == 1).Slug != "cordless-hammer-drill")
    throw new Exception("Canonical product URL changed.");

int rejected = 0;
void Reject(Action<JsonNode> mutate)
{
    JsonNode node = JsonNode.Parse(source)!;
    mutate(node);
    try { CatalogSnapshot.Parse(node.ToJsonString()); }
    catch (InvalidDataException) { rejected++; return; }
    throw new Exception("Invalid catalog was accepted.");
}
Reject(n => n["schemaVersion"] = 99);
Reject(n => n["products"]![0]!["id"] = 2);
Reject(n => n["products"]![0]!["code"] = "HN-002");
Reject(n => n["products"]![0]!["brandId"] = "missing");
Reject(n => n["products"]![0]!["categoryId"] = "missing");
Reject(n => n["products"]![0]!["categoryId"] = "drill-drivers");
Reject(n => n["categories"]!["cordless-drill"]!["parentId"] = "tools-and-equipment");
Reject(n => n["categories"]!["hammer-drills"]!["en"] = "");
Reject(n => n["categories"]!["hammer-drills"]!["slug"] = "Invalid Slug");
Reject(n => n["categories"]!["drill-drivers"]!["slug"] = "hammer-drills");
Reject(n => n["categories"]!.AsObject().Remove("power-tools"));
Reject(n => n["products"]![0]!["detailId"] = "missing");
Reject(n => n["products"]![0]!["mediaId"] = "missing");
Reject(n => n["products"]![0]!["colorIds"]![0] = "missing");
Reject(n => n["products"]![0]!["slug"] = "Invalid Slug");
Reject(n => n["products"]![0]!["price"] = -1);
Reject(n => n["brands"]!["bosch"]!["countryCode"] = "MISSING");
Reject(n => n["media"]!["product-1"]!["main"] = "content/products/images/main/../file.png");
Reject(n => n["media"]!["product-1"]!["card"] = "Content/Bad.jpg");
Reject(n => n["collections"]!["selected"]![0] = 999);
Reject(n => n["products"]![0]!["maxCartQuantity"] = 0);
Reject(n => n["products"]![0]!["discountPercent"] = 100);
Reject(n => n["products"]![0]!["accessories"]![1]!["id"] = "shipping-insurance");
Reject(n => n["products"]![0]!["accessories"]![0]!["price"] = -1);

JsonNode injection = JsonNode.Parse(source)!;
injection["products"]![0]!["name"] = "</script><script>alert(1)</script>";
CatalogSnapshot safe = CatalogSnapshot.Parse(injection.ToJsonString());
if (safe.BrowserJson.Contains("<script", StringComparison.OrdinalIgnoreCase) || safe.BrowserJson.Contains("</script", StringComparison.OrdinalIgnoreCase))
    throw new Exception("Catalog JSON can escape its script element.");
Console.WriteLine($"C# catalog contracts OK: {rejected} invalid catalogs rejected; canonical identity and safe JSON verified.");
