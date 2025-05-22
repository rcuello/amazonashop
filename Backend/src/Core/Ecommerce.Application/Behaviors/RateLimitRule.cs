namespace Ecommerce.Application.Behaviors
{
    /// <summary>
    /// Regla de rate limiting
    /// </summary>
    public class RateLimitRule
    {
        public int MaxRequests { get; set; }
        public int WindowMinutes { get; set; }
    }
}
