namespace Ecommerce.Application.Configuration;

public class RateLimitingConfiguration
{
    public const string SectionName = "RateLimiting";

    public GlobalLimitOptions Global { get; set; } = new();
    public Dictionary<string, PolicyOptions> Policies { get; set; } = new();
    public RejectionOptions Rejection { get; set; } = new();
    public bool EnableMetrics { get; set; } = true;
    public List<string> WhitelistedIPs { get; set; } = new();
    public List<string> WhitelistedUserAgents { get; set; } = new();
}
