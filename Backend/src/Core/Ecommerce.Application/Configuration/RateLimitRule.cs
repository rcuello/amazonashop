using System.ComponentModel.DataAnnotations;


namespace Ecommerce.Application.Configuration
{
    public class RateLimitRule
    {
        [Range(1, int.MaxValue, ErrorMessage = "MaxRequests debe ser mayor a 0")]
        public int MaxRequests { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "WindowMinutes debe ser mayor a 0")]
        public int WindowMinutes { get; set; }

        /// <summary>
        /// Descripción opcional de la regla
        /// </summary>
        public string? Description { get; set; }
    }
}
