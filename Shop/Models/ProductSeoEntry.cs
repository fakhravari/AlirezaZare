namespace Shop.Models;

public sealed record ProductSeoEntry(
    int Id,
    string Name,
    string EnglishName,
    string Brand,
    string Type,
    string ImagePath,
    string Slug,
    string Code,
    IReadOnlyList<ProductCategorySeoEntry> CategoryPath)
{
    public string Description =>
        $"{Name} از برند {Brand}، در گروه {Type}. مشخصات فنی، تصاویر، ویژگی‌های قابل انتخاب و اقلام همراه این کالا را بررسی کنید.";
}

public sealed record ProductCategorySeoEntry(string Name, string Slug);
