using System.Net;
using System.Text.Json;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Exceptions.App;
using Ecommerce.Application.Models.Api;

namespace Ecommerce.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment environment,
            JsonSerializerOptions jsonOptions
    )
    {
        _next = next;
        _logger = logger;
        _environment = environment;

        _jsonOptions = jsonOptions;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await _next(context);
        }
        catch (Exception exception)
        {
            // Registrar la excepción con un ID de correlación para seguimiento
            var correlationId = context.TraceIdentifier;
            
            if (IsClientError(exception))
            {
                _logger.LogWarning("Client error {CorrelationId}: {ExceptionType} - {ExceptionMessage}",
                    correlationId, exception.GetType().Name, exception.Message);
            }
            else
            {
                _logger.LogError(exception, "Server error {CorrelationId}: {ExceptionType} - {ExceptionMessage}",
                    correlationId, exception.GetType().Name, exception.Message);
            }

            // Asegurarse de que la respuesta no se haya enviado ya
            if (!context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, exception, correlationId);
            }
            else
            {
                _logger.LogWarning("No se pudo enviar la respuesta de error porque la respuesta ya ha comenzado");
            }
        }
    }

    private bool IsClientError(Exception exception)
    {
        if (exception is ApplicationExceptionBase appEx)
        {
            return (int)appEx.StatusCode >= 400 && (int)appEx.StatusCode < 500;
        }

        return exception is FluentValidation.ValidationException;
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json";

        // Obtener el código de estado y mensajes apropiados para esta excepción
        var (statusCode, messages) = GetStatusCodeAndMessages(exception);
        context.Response.StatusCode = (int)statusCode;

        // Crear la respuesta de error
        var errorResponse = new ApiErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = messages,
            TraceId = correlationId
        };

        // Agregar detalles adicionales en entorno de desarrollo
        if (_environment.IsDevelopment())
        {
            errorResponse.Details = exception.StackTrace;

            var innerExceptions = GetInnerExceptions(exception);
            if (innerExceptions.Any())
            {
                errorResponse.InnerErrors = innerExceptions;
            }
        }

        if(exception is RateLimitExceededException rateLimitExceededException)
        {
            var retryAfterSeconds = (rateLimitExceededException.WindowMinutes * 60).ToString();

            context.Response.Headers["Retry-After"] = retryAfterSeconds;
            context.Response.Headers["X-RateLimit-Limit"] = rateLimitExceededException.MaxRequests.ToString();
            //context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["X-RateLimit-Window"] = rateLimitExceededException.WindowMinutes.ToString();
            context.Response.Headers["X-RateLimit-RequestType"] = rateLimitExceededException.RequestTypeName ?? "Unknown";
        }

        // Serializar y escribir la respuesta usando System.Text.Json
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, _jsonOptions));
    }



    private (HttpStatusCode StatusCode, string[] Messages) GetStatusCodeAndMessages(Exception exception)
    {
        return exception switch
        {
            // Manejo automático de todas las ApplicationExceptionBase usando su StatusCode
            ApplicationExceptionBase appEx => (appEx.StatusCode, new[] { appEx.Message }),

            // Casos especiales que no heredan de ApplicationExceptionBase
            FluentValidation.ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                validationEx.Errors.Select(e => e.ErrorMessage).ToArray()
            ),

            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                new[] { "La operación ha excedido el tiempo de espera." }
            ),

            // Fallback para excepciones no manejadas
            _ => (HttpStatusCode.InternalServerError, new[] { "Ha ocurrido un error interno." })
        };
    }

    private List<string> GetInnerExceptions(Exception exception)
    {
        var innerExceptions = new List<string>();
        var currentException = exception.InnerException;

        while (currentException != null)
        {
            innerExceptions.Add($"{currentException.GetType().Name}: {currentException.Message}");
            currentException = currentException.InnerException;
        }

        return innerExceptions;
    }

}