using Ecommerce.Shared.Logging;
using Serilog;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class LoggerExtensions
{
    public static ConfigureHostBuilder UseCustomLogger(this ConfigureHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureSerilog();

        return hostBuilder;
    }

    public static IServiceCollection AddCustomLogger(this IServiceCollection services)
    {
        services.AddLogging(logger => logger.AddSerilog());

        return services;
    }
}
