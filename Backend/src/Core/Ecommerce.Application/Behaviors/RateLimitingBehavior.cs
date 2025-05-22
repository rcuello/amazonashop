using Ecommerce.Application.Exceptions.App;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<string, RateLimitRule> _rateLimitRules = new();

        public RateLimitingBehavior(
        IMemoryCache cache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RateLimitingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            InitializeRateLimitRules();
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestType = typeof(TRequest);
            var rule = GetRateLimitRule(requestType);

            if (rule != null)
            {
                var clientId = GetClientIdentifier();
                var key = $"rate_limit:{requestType.Name}:{clientId}";

                if (!await CheckRateLimit(key, rule))
                {
                    _logger.LogWarning("Rate limit exceeded for {RequestType} by client {ClientId}",
                        requestType.Name, clientId);

                    throw new RateLimitExceededException($"Rate limit exceeded for {requestType.Name}");
                }

                _logger.LogDebug("Rate limit check passed for {RequestType} by client {ClientId}",
                    requestType.Name, clientId);
            }

            return await next();
        }

        private void InitializeRateLimitRules()
        {
            // Reglas específicas por tipo de comando/query
            _rateLimitRules.TryAdd("LoginUserCommand", new RateLimitRule { MaxRequests = 5, WindowMinutes = 1 });
            _rateLimitRules.TryAdd("RegisterUserCommand", new RateLimitRule { MaxRequests = 3, WindowMinutes = 5 });
            _rateLimitRules.TryAdd("ResetPasswordCommand", new RateLimitRule { MaxRequests = 2, WindowMinutes = 60 });
            _rateLimitRules.TryAdd("ResetPasswordByTokenCommand", new RateLimitRule { MaxRequests = 3, WindowMinutes = 5 });
            _rateLimitRules.TryAdd("SendPasswordCommand", new RateLimitRule { MaxRequests = 2, WindowMinutes = 60 });

            // Commands (operaciones de escritura)
            _rateLimitRules.TryAdd("UpdateUserCommand", new RateLimitRule { MaxRequests = 10, WindowMinutes = 1 });
            _rateLimitRules.TryAdd("UpdateAdminUserCommand", new RateLimitRule { MaxRequests = 5, WindowMinutes = 1 });
            _rateLimitRules.TryAdd("UpdateAdminStatusUserCommand", new RateLimitRule { MaxRequests = 2, WindowMinutes = 5 });

            // Queries (operaciones de lectura) - más permisivas
            _rateLimitRules.TryAdd("GetUserByIdQuery", new RateLimitRule { MaxRequests = 100, WindowMinutes = 1 });
            _rateLimitRules.TryAdd("GetUserByTokenQuery", new RateLimitRule { MaxRequests = 50, WindowMinutes = 1 });
            _rateLimitRules.TryAdd("GetUserByUsernameQuery", new RateLimitRule { MaxRequests = 30, WindowMinutes = 1 });
            _rateLimitRules.TryAdd("PaginationUsersQuery", new RateLimitRule { MaxRequests = 10, WindowMinutes = 1 });
            _rateLimitRules.TryAdd("GetRolesQuery", new RateLimitRule { MaxRequests = 20, WindowMinutes = 1 });
        }

        private RateLimitRule? GetRateLimitRule(Type requestType)
        {
            var requestName = requestType.Name;

            // Buscar regla específica
            if (_rateLimitRules.TryGetValue(requestName, out var specificRule))
            {
                return specificRule;
            }

            // Reglas por categoría usando convenciones de nombre
            if (requestName.EndsWith("Command"))
            {
                // Comandos críticos de seguridad
                if (requestName.Contains("Password") || requestName.Contains("Reset") || requestName.Contains("Send"))
                {
                    return new RateLimitRule { MaxRequests = 3, WindowMinutes = 5 };
                }

                // Comandos administrativos
                if (requestName.Contains("Admin"))
                {
                    return new RateLimitRule { MaxRequests = 10, WindowMinutes = 1 };
                }

                // Comandos generales de escritura
                return new RateLimitRule { MaxRequests = 20, WindowMinutes = 1 };
            }

            if (requestName.EndsWith("Query"))
            {
                // Queries de paginación (más costosas)
                if (requestName.Contains("Pagination") || requestName.Contains("List"))
                {
                    return new RateLimitRule { MaxRequests = 15, WindowMinutes = 1 };
                }

                // Queries generales de lectura
                return new RateLimitRule { MaxRequests = 60, WindowMinutes = 1 };
            }

            return null; // Sin rate limiting
        }

        private async Task<bool> CheckRateLimit(string key, RateLimitRule rule)
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

            lock (requestLog)
            {
                // Limpiar requests antiguos
                requestLog.RemoveAll(timestamp => timestamp < windowStart);

                // Verificar si excede el límite
                if (requestLog.Count >= rule.MaxRequests)
                {
                    return false;
                }

                // Agregar el request actual
                requestLog.Add(currentTime);

                // Actualizar el cache
                _cache.Set(key, requestLog, TimeSpan.FromMinutes(rule.WindowMinutes + 1));
            }

            return true;
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
