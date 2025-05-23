using Ecommerce.Application.Configuration;
using Ecommerce.Application.Exceptions.App;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Ecommerce.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior para aplicar rate limiting a nivel de MediatR handlers
    /// </summary>
    public class RateLimitingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RateLimitingBehavior<TRequest, TResponse>> _logger;
        private readonly RateLimitConfiguration _config;        

        public RateLimitingBehavior(
            IOptions<RateLimitConfiguration> rateLimitOptions,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor,
            ILogger<RateLimitingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _config = rateLimitOptions.Value;            
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Si está deshabilitado globalmente, continuar sin rate limiting
            if (!_config.Enabled)
            {
                return await next();
            }

            var requestType = typeof(TRequest);
            var rule = GetRateLimitRule(requestType);

            if (rule != null)
            {
                var clientId = GetClientIdentifier();
                var key = $"rate_limit:{requestType.Name}:{clientId}";

                if (!await CheckRateLimit(key, rule))
                {
                    _logger.LogWarning("Rate limit exceeded for {RequestType} by client {ClientId}. Rule: {MaxRequests} requests per {WindowMinutes} minutes",
                        requestType.Name, clientId, rule.MaxRequests, rule.WindowMinutes);

                    throw new RateLimitExceededException($"Rate limit exceeded for {requestType.Name}. Maximum {rule.MaxRequests} requests per {rule.WindowMinutes} minutes allowed.");
                }

                _logger.LogDebug("Rate limit check passed for {RequestType} by client {ClientId}",
                    requestType.Name, clientId);
            }

            return await next();
        }        

        private RateLimitRule? GetRateLimitRule(Type requestType)
        {
            var requestName = requestType.Name;

            // 1. Buscar regla específica en configuración
            if (_config.SpecificRules.TryGetValue(requestName, out var specificRule))
            {
                return specificRule;
            }

            // 2. Aplicar reglas por categoría usando convenciones de nombre
            if (requestName.EndsWith("Command"))
            {
                // Comandos críticos de seguridad
                if (requestName.Contains("Password") || requestName.Contains("Reset") || requestName.Contains("Send"))
                {
                    return _config.Categories.SecurityCommands;
                }

                // Comandos administrativos
                if (requestName.Contains("Admin"))
                {
                    return _config.Categories.AdminCommands;
                }

                // Comandos generales de escritura
                return _config.Categories.GeneralCommands;
            }

            if (requestName.EndsWith("Query"))
            {
                // Queries de paginación (más costosas)
                if (requestName.Contains("Pagination") || requestName.Contains("List"))
                {
                    return _config.Categories.PaginationQueries;
                }

                // Queries generales de lectura
                return _config.Categories.GeneralQueries;
            }

            // 3. Regla por defecto si está configurada
            return _config.DefaultRule;
        }

        private Task<bool> CheckRateLimit(string key, RateLimitRule rule)
        {
            var currentTime = DateTime.UtcNow;
            var windowStart = currentTime.AddMinutes(-rule.WindowMinutes);

            // Obtener el contador actual
            // builder.Services.AddMemoryCache: Con SizeLimit (requerirá Size en cada entrada)
            // Sino es especificado probablemente se lance el error:
            // System.InvalidOperationException: 'Cache entry must specify a value for Size when SizeLimit is set.'
            var requestLog = _cache.GetOrCreate(key, factory =>
            {
                factory.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(rule.WindowMinutes + 1);
                return new List<DateTime>();
            });

            if (requestLog == null)
            {
                requestLog = new List<DateTime>();
                _cache.Set(key, requestLog, TimeSpan.FromMinutes(rule.WindowMinutes + 1));
            }

            lock (requestLog)
            {
                // Limpiar requests antiguos
                requestLog.RemoveAll(timestamp => timestamp < windowStart);

                // Verificar si excede el límite
                if (requestLog.Count >= rule.MaxRequests)
                {
                    return Task.FromResult(false);
                }

                // Agregar el request actual
                requestLog.Add(currentTime);

                // Actualizar el cache
                _cache.Set(key, requestLog, TimeSpan.FromMinutes(rule.WindowMinutes + 1));
            }

            return Task.FromResult(true);
        }

        private string GetClientIdentifier()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "system";

            // Priorizar usuario autenticado
            var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.User?.FindFirst("sub")?.Value
                        ?? httpContext.User?.Identity?.Name;

            if (!string.IsNullOrEmpty(userId))
                return $"user:{userId}";

            // Usar IP como fallback
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(clientIp))
                return $"ip:{clientIp}";

            // Último recurso: User-Agent hash
            var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();
            return $"agent:{userAgent?.GetHashCode().ToString() ?? "unknown"}";
        }
    }
}
