namespace Ecommerce.Application.Configuration.Models;

public class JaegerOptions
{
    public string AgentHost { get; set; } = "localhost";
    public int AgentPort { get; set; } = 14268;
    public string Endpoint { get; set; } = "http://localhost:14268/api/traces";
}
