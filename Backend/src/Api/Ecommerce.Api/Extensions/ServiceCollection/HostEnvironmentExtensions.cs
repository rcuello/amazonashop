namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class HostEnvironmentExtensions
{
    public static bool IsDevelopmentOrLocal(this IHostEnvironment environment)
    {
        return environment.IsEnvironment("Development") || environment.IsEnvironment("Local");
    }    
}
