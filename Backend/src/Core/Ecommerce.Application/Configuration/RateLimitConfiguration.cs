
namespace Ecommerce.Application.Configuration
{
    /// <summary>
    /// Configuración de rate limiting por ambiente
    /// </summary>
    public class RateLimitConfiguration
    {
        public const string SectionName = "RateLimit";

        /// <summary>
        /// Habilitar/deshabilitar rate limiting globalmente
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Configuración específica por comando/query
        /// </summary>
        public Dictionary<string, RateLimitRule> SpecificRules { get; set; } = new();

        /// <summary>
        /// Configuración por categorías (fallback)
        /// </summary>
        public CategoryRules Categories { get; set; } = new();

        /// <summary>
        /// Configuración por defecto cuando no hay regla específica
        /// </summary>
        public RateLimitRule? DefaultRule { get; set; }
    }
}
