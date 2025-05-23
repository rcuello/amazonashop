using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Ecommerce.Application.Models.Api;
using Ecommerce.Application.Configuration;
using System.Net;
using System.Text.Json;


namespace Ecommerce.Api.Extensions.ServiceCollection
{
    public static class RateLimitingExtensions
    {
        public static WebApplication UseCustomRateLimiter(this WebApplication app)
        {
            app.UseRateLimiter();

            return app;
        }

        public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RateLimitingConfiguration>(configuration.GetSection(RateLimitingConfiguration.SectionName));

            services.AddRateLimiter(options =>
            {
                var rateLimitConfig = configuration.GetSection(RateLimitingConfiguration.SectionName).Get<RateLimitingConfiguration>()
                                     ?? new RateLimitingConfiguration();

                // Configurar limitador global
                if (rateLimitConfig.Global.Enabled)
                {
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                        httpContext =>
                        {
                            if (IsWhitelisted(httpContext, rateLimitConfig))
                                return RateLimitPartition.GetNoLimiter("whitelist");

                            return RateLimitPartition.GetFixedWindowLimiter(
                                partitionKey: GetClientIdentifier(httpContext),
                                factory: partition => new FixedWindowRateLimiterOptions
                                {
                                    AutoReplenishment = true,
                                    PermitLimit = rateLimitConfig.Global.PermitLimit,
                                    Window = TimeSpan.FromMinutes(rateLimitConfig.Global.WindowMinutes)
                                });
                        });
                }

                // Configurar políticas específicas
                ConfigurePolicies(options, rateLimitConfig);

                // Configurar respuesta de rechazo
                options.OnRejected = CreateRejectionHandler(rateLimitConfig);
            });

            return services;
        }

        private static void ConfigurePolicies(RateLimiterOptions options, RateLimitingConfiguration config)
        {
            foreach (var policy in config.Policies)
            {
                if (!policy.Value.Enabled) continue;

                options.AddPolicy(policy.Key, httpContext =>
                {
                    if (IsWhitelisted(httpContext, config))
                        return RateLimitPartition.GetNoLimiter("whitelist");

                    return policy.Value.Type.ToLowerInvariant() switch
                    {
                        "fixedwindow" => CreateFixedWindowPartition(httpContext, policy.Value),
                        "slidingwindow" => CreateSlidingWindowPartition(httpContext, policy.Value),
                        "tokenbucket" => CreateTokenBucketPartition(httpContext, policy.Value),
                        "concurrency" => CreateConcurrencyPartition(httpContext, policy.Value),
                        _ => CreateFixedWindowPartition(httpContext, policy.Value)
                    };
                });
            }
        }

        private static Func<OnRejectedContext, CancellationToken, ValueTask> CreateRejectionHandler(RateLimitingConfiguration config)
        {
            return async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.Append("X-Content-Type-Options", "nosniff");

                if (config.Rejection.IncludeHeaders)
                {
                    // Agregar headers informativos
                    if (config.Rejection.IncludeRetryAfter &&
                        context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                    }

                    // Obtener información de la política aplicada
                    var endpoint = context.HttpContext.GetEndpoint();
                    var policyName = endpoint?.Metadata?.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ?? "global";

                    context.HttpContext.Response.Headers.Append("X-RateLimit-Policy", policyName);
                    context.HttpContext.Response.Headers.Append("X-RateLimit-Limit", "exceeded");
                    context.HttpContext.Response.Headers.Append("X-RateLimit-Client", GetClientIdentifier(context.HttpContext));
                }

                // Log del rechazo si está habilitado
                /*if (config.Rejection.LogRejections)
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<RateLimitingExtensions>>();
                    logger?.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}",
                        GetClientIdentifier(context.HttpContext),
                        context.HttpContext.Request.Path);
                }*/

                // Crear respuesta de error
                var message = !string.IsNullOrEmpty(config.Rejection.CustomMessage)
                    ? config.Rejection.CustomMessage
                    : "Rate limit exceeded. Too many requests.";

                var errorResponse = new ApiErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.TooManyRequests,
                    Message = [message],
                    TraceId = context.HttpContext.TraceIdentifier
                };

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }),
                    cancellationToken: token);

                // Actualizar métricas si están habilitadas
                //var metrics = context.HttpContext.RequestServices.GetService<IRateLimitingMetrics>();
                //metrics?.IncrementRejection(GetClientIdentifier(context.HttpContext));
            };
        }

        private static RateLimitPartition<string> CreateFixedWindowPartition(HttpContext httpContext, PolicyOptions policy)
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetClientIdentifier(httpContext),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = policy.AutoReplenishment,
                    PermitLimit = policy.PermitLimit,
                    Window = policy.WindowSeconds > 0
                        ? TimeSpan.FromSeconds(policy.WindowSeconds)
                        : TimeSpan.FromMinutes(policy.WindowMinutes)
                });
        }

        private static RateLimitPartition<string> CreateSlidingWindowPartition(HttpContext httpContext, PolicyOptions policy)
        {
            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: GetClientIdentifier(httpContext),
                factory: partition => new SlidingWindowRateLimiterOptions
                {
                    AutoReplenishment = policy.AutoReplenishment,
                    PermitLimit = policy.PermitLimit,
                    Window = policy.WindowSeconds > 0
                        ? TimeSpan.FromSeconds(policy.WindowSeconds)
                        : TimeSpan.FromMinutes(policy.WindowMinutes),
                    SegmentsPerWindow = policy.SegmentsPerWindow
                });
        }

        private static RateLimitPartition<string> CreateTokenBucketPartition(HttpContext httpContext, PolicyOptions policy)
        {
            return RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: GetClientIdentifier(httpContext),
                factory: partition => new TokenBucketRateLimiterOptions
                {
                    AutoReplenishment = policy.AutoReplenishment,
                    TokenLimit = policy.PermitLimit,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(policy.ReplenishmentSeconds),
                    TokensPerPeriod = policy.TokensPerPeriod
                });
        }

        private static RateLimitPartition<string> CreateConcurrencyPartition(HttpContext httpContext, PolicyOptions policy)
        {
            return RateLimitPartition.GetConcurrencyLimiter(
                partitionKey: GetClientIdentifier(httpContext),
                factory: partition => new ConcurrencyLimiterOptions
                {
                    PermitLimit = policy.PermitLimit,
                    QueueLimit = policy.QueueLimit,
                    QueueProcessingOrder = policy.QueueProcessingOrder.ToLowerInvariant() == "newestfirst"
                        ? QueueProcessingOrder.NewestFirst
                        : QueueProcessingOrder.OldestFirst
                });
        }

        private static bool IsWhitelisted(HttpContext httpContext, RateLimitingConfiguration config)
        {
            // Verificar IP whitelist
            var clientIp = GetClientIpAddress(httpContext);
            if (!string.IsNullOrEmpty(clientIp) && config.WhitelistedIPs.Contains(clientIp))
                return true;

            // Verificar User-Agent whitelist
            var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();
            if (!string.IsNullOrEmpty(userAgent) &&
                config.WhitelistedUserAgents.Any(ua => userAgent.Contains(ua, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        private static string GetClientIdentifier(HttpContext httpContext)
        {
            // Prioridad: Usuario autenticado > IP del cliente > User-Agent hash
            var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.User?.FindFirst("sub")?.Value
                        ?? httpContext.User?.Identity?.Name;

            if (!string.IsNullOrEmpty(userId))
                return $"user:{userId}";

            var clientIp = GetClientIpAddress(httpContext);
            if (!string.IsNullOrEmpty(clientIp))
                return $"ip:{clientIp}";

            var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();
            return $"agent:{userAgent?.GetHashCode().ToString() ?? "unknown"}";
        }

        private static string GetClientIpAddress(HttpContext httpContext)
        {
            // Verificar headers de proxy/load balancer
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                return ips[0].Trim();
            }

            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            return httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        }
    }
}
