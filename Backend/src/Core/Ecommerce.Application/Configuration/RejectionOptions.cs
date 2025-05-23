namespace Ecommerce.Application.Configuration;

public class RejectionOptions
{
    public bool IncludeRetryAfter { get; set; } = true;
    public bool IncludeHeaders { get; set; } = true;
    public bool LogRejections { get; set; } = true;
    public string CustomMessage { get; set; } = string.Empty;
}
