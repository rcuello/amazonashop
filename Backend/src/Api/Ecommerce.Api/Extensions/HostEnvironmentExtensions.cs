namespace Ecommerce.Api.Extensions;

public static class HostEnvironmentExtensions
{
    public static bool IsDevelopmentOrLocal(this IHostEnvironment environment)
    {
        return environment.IsEnvironment("Development") || environment.IsEnvironment("Local");
    }    
}
