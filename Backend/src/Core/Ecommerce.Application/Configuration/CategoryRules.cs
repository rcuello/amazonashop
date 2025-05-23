namespace Ecommerce.Application.Configuration
{
    public class CategoryRules
    {
        /// <summary>
        /// Reglas para comandos críticos de seguridad (Password, Reset, etc.)
        /// </summary>
        public RateLimitRule SecurityCommands { get; set; } = new() { MaxRequests = 3, WindowMinutes = 5 };

        /// <summary>
        /// Reglas para comandos administrativos
        /// </summary>
        public RateLimitRule AdminCommands { get; set; } = new() { MaxRequests = 10, WindowMinutes = 1 };

        /// <summary>
        /// Reglas para comandos generales
        /// </summary>
        public RateLimitRule GeneralCommands { get; set; } = new() { MaxRequests = 20, WindowMinutes = 1 };

        /// <summary>
        /// Reglas para queries de paginación/listado
        /// </summary>
        public RateLimitRule PaginationQueries { get; set; } = new() { MaxRequests = 15, WindowMinutes = 1 };

        /// <summary>
        /// Reglas para queries generales
        /// </summary>
        public RateLimitRule GeneralQueries { get; set; } = new() { MaxRequests = 60, WindowMinutes = 1 };
    }
}
