namespace Ecommerce.Application.Configuration.Models;

public class ZipkinOptions
{
    public string Endpoint { get; set; } = "http://localhost:9411/api/v2/spans";
}
