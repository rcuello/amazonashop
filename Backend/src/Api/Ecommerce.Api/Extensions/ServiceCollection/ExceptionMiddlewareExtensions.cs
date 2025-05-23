using Ecommerce.Api.Middlewares;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class ExceptionMiddlewareExtensions
{
    public static WebApplication UseCustomExceptionMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();

        return app;
    }
}
