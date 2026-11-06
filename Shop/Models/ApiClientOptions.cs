namespace Shop.Models;

public sealed class ApiClientOptions
{
    public string BasePath { get; set; } = "~/api/";
    public int TimeoutMilliseconds { get; set; } = 15000;
}
