using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ecommerce.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Executing {RequestName}) with data {@Request}", requestName, request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation("Successfully executed {RequestName} in {ElapsedMs}ms",requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Failed to execute {RequestName} in {ElapsedMs}ms",requestName, stopwatch.ElapsedMilliseconds);

            // Re-throw para que el ExceptionMiddleware lo maneje
            throw; 
        }
    }
}
/*
 Este pipeline es complementario al ExceptionMiddleware (API)

 # Ecommerce.Api.Middlewares.ExceptionMiddleware
    ✅ Scope     : Toda la aplicación HTTP
    ✅ Propósito : Convertir excepciones → respuestas HTTP
    ✅ Nivel     : Infrastructure/Web

 # Ecommerce.Application.Behaviors.LoggingBehavior
    ✅ Scope     : Scope: Solo Commands/Queries de MediatR
    ✅ Propósito :Logging de lógica de negocio específica
    ✅ Nivel     : Application/Business

 #  Flujo Completo
    HTTP Request 
        ↓
    RequestLoggingMiddleware → "POST /api/products started"
        ↓  
    Controller → mediator.Send(command)
        ↓
    LoggingBehavior → "Executing CreateProductCommand for user123"
        ↓
    Handler → Lógica de negocio
        ↓
    LoggingBehavior → "Successfully executed in 150ms"
        ↓
    RequestLoggingMiddleware → "POST responded 201 in 180ms"

# Si hay error:
    Handler → throws ValidationException
        ↓
    LoggingBehavior → "Failed to execute CreateProductCommand"
        ↓
    ExceptionMiddleware → "Client error: ValidationException"
        ↓
    HTTP Response 400 + JSON error

 */
