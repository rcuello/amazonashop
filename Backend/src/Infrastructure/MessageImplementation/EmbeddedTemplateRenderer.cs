using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System.Reflection;
using System.Text;

namespace Ecommerce.Infrastructure.MessageImplementation
{
    public class EmbeddedTemplateRenderer : ITemplateRender
    {
        private readonly ILogger<EmbeddedTemplateRenderer> _logger;
        private readonly TemplateRendererOptions _options;
        private readonly SemaphoreSlim _compilationLock;
        private readonly IMemoryCache _templateCache;
        private readonly Assembly _resourceAssembly;
        private readonly string _assemblyName;
        private readonly string[] _availableResources;
        private bool _disposed;

        public EmbeddedTemplateRenderer(
            IOptions<TemplateRendererOptions> options,
            ILogger<EmbeddedTemplateRenderer> logger,
            IMemoryCache memoryCache)
        {
            _options = options?.Value ?? new TemplateRendererOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _templateCache = memoryCache;
            _compilationLock = new SemaphoreSlim(_options.MaxConcurrentCompilations, _options.MaxConcurrentCompilations);

            // Obtener la información de la asamblea que contiene los recursos
            _resourceAssembly = Assembly.GetExecutingAssembly();
            _assemblyName = _resourceAssembly.GetName().Name!;

            // Precargamos todos los nombres de recursos disponibles para diagnóstico
            _availableResources = _resourceAssembly.GetManifestResourceNames();

            _logger.LogInformation("Inicializando EmbeddedTemplateRenderer (Scriban) desde la asamblea {AssemblyName}", _assemblyName);

            // Registrar los recursos disponibles que coincidan con EmailTemplates para diagnóstico
            var emailTemplateResources = _availableResources
                .Where(r => r.Contains("EmailTemplates") || r.Contains("_Embedded"))
                .ToList();

            if (emailTemplateResources.Any())
            {
                _logger.LogInformation("Recursos de plantillas encontrados en la asamblea {AssemblyName}:", _assemblyName);
                foreach (var resource in emailTemplateResources)
                {
                    _logger.LogInformation("  - {ResourceName}", resource);
                }
            }
            else
            {
                _logger.LogWarning("No se encontraron recursos de plantillas en la asamblea {AssemblyName}", _assemblyName);
            }

            _logger.LogInformation("EmbeddedTemplateRenderer inicializado con base path: {BasePath}", _options.BasePath);

            
        }

        /// <summary>
        /// Renderiza una plantilla con el modelo proporcionado
        /// </summary>
        /// <typeparam name="T">Tipo del modelo de datos</typeparam>
        /// <param name="templateName">Nombre de la plantilla (sin extensión)</param>
        /// <param name="model">Modelo de datos para la plantilla</param>
        /// <param name="cancellationToken">Token de cancelación opcional</param>
        /// <returns>Contenido HTML renderizado</returns>
        public async Task<string> RenderAsync<T>(string templateName, T model, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                throw new ArgumentException("El nombre de la plantilla no puede estar vacío", nameof(templateName));
            }

            // Construimos las claves para identificar la plantilla
            string normalizedTemplateName = NormalizeTemplateName(templateName);
            string fullTemplatePath = GetFullResourcePath(normalizedTemplateName);
            string cacheKey = $"Template_{fullTemplatePath}";

            try
            {
                _logger.LogDebug("Intentando renderizar plantilla '{TemplateKey}'", fullTemplatePath);

                // Intentar obtener desde caché primero si está habilitado
                if (_options.EnableCaching && _templateCache.TryGetValue(cacheKey, out Template? cachedTemplate))
                {
                    if (cachedTemplate is null)
                    {
                        throw new TemplateRenderException($"Se esperaba que la plantilla '{templateName}' estuviera en caché, pero era null.");
                    }

                    _logger.LogDebug("Plantilla {TemplateName} obtenida de caché", templateName);

                    return await RenderTemplateWithModelAsync(cachedTemplate, model, cancellationToken);
                }

                // Adquirir semáforo para limitar compilaciones concurrentes
                await _compilationLock.WaitAsync(cancellationToken);

                try
                {
                    // Verificar si la plantilla existe como recurso embebido
                    if (!await TemplateExistsAsync(normalizedTemplateName))
                    {
                        // Mostrar recursos disponibles para mejor diagnóstico
                        var similarResources = _availableResources
                            .Where(r => r.Contains("EmailTemplates") || r.Contains("_Embedded"))
                            .ToList();

                        string resourcesMessage = similarResources.Any()
                            ? $"Recursos similares disponibles: {string.Join(", ", similarResources)}"
                            : "No se encontraron recursos similares en la asamblea.";

                        throw new TemplateRenderException(
                            $"No se encontró la plantilla '{templateName}' en la ruta de recursos embebidos. " +
                            $"Se buscó como '{fullTemplatePath}'. {resourcesMessage}");
                    }

                    // Cargar y compilar la plantilla
                    Template template = await LoadAndCompileTemplateAsync(fullTemplatePath, cancellationToken);

                    if (template == null) throw new TemplateRenderException($"No se pudo compilar la plantilla '{templateName}'");
                    

                    // Almacenar en caché si está habilitado
                    if (_options.EnableCaching)
                    {
                        var cacheEntryOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _options.CacheDuration
                        };

                        // Establecer un tamaño estimado para la entrada de caché si SizeLimit está habilitado
                        cacheEntryOptions.SetSize(1); // Usar un valor básico o calcular basado en el contenido

                        _templateCache.Set(cacheKey, template, cacheEntryOptions);
                    }

                    // Renderizar la plantilla con el modelo
                    return await RenderTemplateWithModelAsync(template, model, cancellationToken);
                }
                finally
                {
                    _compilationLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al renderizar la plantilla {TemplateName}: {ErrorMessage}",
                    templateName, ex.Message);

                // Loguear los recursos disponibles para ayudar en el diagnóstico
                LogResourcesMatchingPattern(normalizedTemplateName);

                throw new TemplateRenderException($"Error al renderizar la plantilla '{templateName}'", ex);
            }
        }

        /// <summary>
        /// Carga y compila una plantilla desde un recurso embebido
        /// </summary>
        private async Task<Template> LoadAndCompileTemplateAsync(string resourcePath, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Cargando plantilla desde recurso: {ResourcePath}", resourcePath);

            // Leer el contenido del recurso embebido
            string templateContent = await ReadEmbeddedResourceAsync(resourcePath, cancellationToken);

            if (string.IsNullOrEmpty(templateContent))
            {
                throw new TemplateRenderException($"El recurso '{resourcePath}' está vacío o no se pudo leer");
            }

            _logger.LogDebug("Compilando plantilla: {ResourcePath}", resourcePath);

            // Opciones de análisis para Scriban
            var parserOptions = new ParserOptions
            {
                //Mode = ScriptMode.Default
            };

            // Compilar la plantilla
            var template = Template.Parse(templateContent, resourcePath, parserOptions);

            if (template.HasErrors)
            {
                var errors = string.Join(Environment.NewLine, template.Messages.Select(m => m.ToString()));
                throw new TemplateRenderException($"Error al compilar la plantilla '{resourcePath}': {errors}");
            }

            return template;
        }

        /// <summary>
        /// Renderiza una plantilla compilada con el modelo dado
        /// </summary>
        private async Task<string> RenderTemplateWithModelAsync<T>(Template template, T model, CancellationToken cancellationToken)
        {
            // Crear un nuevo contexto para la renderización
            var context = new TemplateContext
            {
                LoopLimit = _options.MaxLoopIterations,
                RecursiveLimit = _options.MaxRecursionDepth,
                MemberRenamer = member => member.Name
            };

            // Configurar el modelo en el contexto
            var scriptObject = new ScriptObject();
            scriptObject.Import(model, renamer: member => member.Name);
            context.PushGlobal(scriptObject);

            // Renderizar la plantilla de forma asíncrona
            return await Task.FromResult(template.Render(context));
        }

        /// <summary>
        /// Lee el contenido de un recurso embebido como texto
        /// </summary>
        private async Task<string> ReadEmbeddedResourceAsync(string resourcePath, CancellationToken cancellationToken)
        {
            try
            {
                using var stream = _resourceAssembly.GetManifestResourceStream(resourcePath);
                if (stream == null)
                {
                    throw new TemplateRenderException($"No se pudo abrir el recurso: {resourcePath}");
                }

                using var reader = new StreamReader(stream, Encoding.UTF8);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex) when (!(ex is TemplateRenderException))
            {
                throw new TemplateRenderException($"Error al leer el recurso embebido '{resourcePath}'", ex);
            }
        }

        /// <summary>
        /// Registra recursos que coincidan con un patrón para ayudar en el diagnóstico
        /// </summary>
        private void LogResourcesMatchingPattern(string pattern)
        {
            var matchingResources = _availableResources
                .Where(r => r.Contains("_Embedded") || r.Contains("EmailTemplates") || r.Contains(pattern))
                .ToList();

            if (matchingResources.Any())
            {
                _logger.LogInformation("Recursos disponibles que podrían coincidir con '{Pattern}':", pattern);
                foreach (var resource in matchingResources)
                {
                    _logger.LogInformation("  - {ResourceName}", resource);
                }
            }
            else
            {
                _logger.LogWarning("No se encontraron recursos que coincidan con '{Pattern}'", pattern);
            }
        }

        /// <summary>
        /// Obtiene la ruta completa del recurso embebido
        /// </summary>
        private string GetFullResourcePath(string normalizedTemplateName)
        {
            // Formar la ruta completa del recurso usando el formato correcto
            // El formato debe coincidir con cómo se embeben los recursos en el csproj
            return $"{_assemblyName}._Embedded.EmailTemplates.{normalizedTemplateName}{_options.FileExtension}";
        }

        /// <summary>
        /// Normaliza el nombre de la plantilla para asegurar consistencia
        /// </summary>
        private string NormalizeTemplateName(string templateName)
        {
            // Eliminar extensión si se proporciona
            if (templateName.EndsWith(_options.FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                templateName = templateName.Substring(0, templateName.Length - _options.FileExtension.Length);
            }

            // Reemplazar barras por puntos para formato de recursos embebidos
            templateName = templateName.Replace("/", ".").Replace("\\", ".");

            // Asegurar que no comienza con un punto
            if (templateName.StartsWith("."))
            {
                templateName = templateName.Substring(1);
            }

            return templateName;
        }

        /// <summary>
        /// Comprueba si una plantilla existe como recurso embebido
        /// </summary>
        public async Task<bool> TemplateExistsAsync(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                return false;

            try
            {
                // Normalizar el nombre de la plantilla
                string normalizedName = NormalizeTemplateName(templateName);

                // Obtener la ruta completa del recurso
                string resourcePath = GetFullResourcePath(normalizedName);

                _logger.LogDebug("Verificando existencia de recurso: {ResourcePath}", resourcePath);

                // Verificar si existe en la asamblea - operación síncrona pero envuelta en Task para mantener la interfaz
                using var stream = _resourceAssembly.GetManifestResourceStream(resourcePath);
                return await Task.FromResult(stream != null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al verificar existencia de plantilla {TemplateName}", templateName);
                return false;
            }
        }

        /// <summary>
        /// Precarga todas las plantillas en caché
        /// </summary>
        public async Task PrecacheTemplatesAsync(CancellationToken cancellationToken = default)
        {
            if (!_options.EnableCaching && _options.PreloadTemplates)
            {
                _logger.LogWarning("No se puede precargar plantillas cuando el caché está deshabilitado");
                return;
            }

            // Usar el patrón correcto para encontrar las plantillas
            string templatesPattern = "_Embedded.EmailTemplates";
            string extension = _options.FileExtension;

            _logger.LogInformation("Iniciando precarga de plantillas usando patrón '{Pattern}'...", templatesPattern);
            int count = 0;
            int errors = 0;

            foreach (var resourceName in _availableResources)
            {
                // Solo procesar recursos que sean plantillas
                if (resourceName.Contains(templatesPattern) && resourceName.EndsWith(extension))
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // Extraer nombre de plantilla normalizado
                        string templateName = resourceName
                            .Replace($"{_assemblyName}._Embedded.EmailTemplates.", "")
                            .Replace(extension, "");

                        string cacheKey = $"Template_{resourceName}";

                        _logger.LogDebug("Intentando precargar plantilla: {TemplateName}", templateName);

                        // Cargar y compilar la plantilla
                        Template template = await LoadAndCompileTemplateAsync(resourceName, cancellationToken);

                        // Almacenar en caché
                        if (_options.EnableCaching)
                        {
                            var cacheEntryOptions = new MemoryCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = _options.CacheDuration
                            };

                            // Establecer un tamaño estimado para la entrada en caché
                            // Puedes calcular un tamaño basado en el contenido o usar un valor fijo
                            cacheEntryOptions.SetSize(1); // O calcular basado en template.ToString().Length

                            _templateCache.Set(cacheKey, template, cacheEntryOptions);
                        }

                        count++;
                        _logger.LogDebug("Plantilla precargada con éxito: {TemplateName}", templateName);
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        _logger.LogError(ex, "Error al precargar plantilla {ResourceName}: {ErrorMessage}",
                            resourceName, ex.Message);
                    }
                }
            }

            _logger.LogInformation("Precarga de plantillas completada: {SuccessCount} éxitos, {ErrorCount} errores",
                count, errors);
        }

        /// <summary>
        /// Limpia la caché de plantillas
        /// </summary>
        public void ClearCache()
        {
            if (_options.EnableCaching)
            {
                if (_templateCache is MemoryCache memoryCache)
                {
                    memoryCache.Compact(1.0);
                    _logger.LogInformation("Caché de plantillas limpiada");
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _compilationLock?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}