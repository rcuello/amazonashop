
namespace Ecommerce.Infrastructure.MessageImplementation
{
    /// <summary>
    /// Opciones de configuración para el renderizador de plantillas
    /// </summary>
    public class TemplateRendererOptions
    {
        /// <summary>
        /// Ruta base donde se encuentran las plantillas embebidas
        /// Por ejemplo: "_Embedded/EmailTemplates"
        /// </summary>
        public string BasePath { get; set; } = "_Embedded/EmailTemplates";

        /// <summary>
        /// Extensión de los archivos de plantilla
        /// </summary>
        public string FileExtension { get; set; } = ".html";

        /// <summary>
        /// Indica si se debe habilitar el caché de plantillas
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Indica si se deben precargar las plantillas al inicializar el servicio
        /// </summary>
        public bool PreloadTemplates { get; set; } = true;

        /// <summary>
        /// Duración del caché de plantillas
        /// </summary>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Número máximo de compilaciones concurrentes permitidas
        /// </summary>
        public int MaxConcurrentCompilations { get; set; } = 4;

        /// <summary>
        /// Límite máximo de iteraciones en bucles para Scriban
        /// </summary>
        public int MaxLoopIterations { get; set; } = 1000;

        /// <summary>
        /// Profundidad máxima de recursión para Scriban
        /// </summary>
        public int MaxRecursionDepth { get; set; } = 100;
    }
}