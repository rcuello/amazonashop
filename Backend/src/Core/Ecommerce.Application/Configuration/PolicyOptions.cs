namespace Ecommerce.Application.Configuration;

public class PolicyOptions
{
    public string Type { get; set; } = "FixedWindow"; // FixedWindow, SlidingWindow, TokenBucket, Concurrency
    public int PermitLimit { get; set; } = 10;
    public int WindowMinutes { get; set; } = 1;
    public int WindowSeconds { get; set; } = 0;

    // Para TokenBucket
    public int TokensPerPeriod { get; set; } = 5;
    public int ReplenishmentSeconds { get; set; } = 10;

    // Para SlidingWindow
    public int SegmentsPerWindow { get; set; } = 6;

    // Para Concurrency
    public int QueueLimit { get; set; } = 10;
    public string QueueProcessingOrder { get; set; } = "OldestFirst"; // OldestFirst, NewestFirst

    public bool AutoReplenishment { get; set; } = true;
    public bool Enabled { get; set; } = true;
}
