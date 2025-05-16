using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Exceptions.App;
using Ecommerce.Application.Models.Api;
using SendGrid.Helpers.Errors.Model;

namespace Ecommerce.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly Dictionary<Type, Func<Exception, (HttpStatusCode StatusCode, string[] Messages)>> _exceptionHandlers;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment environment
    )
    {
        _next = next;
        _logger = logger;
        _environment = environment;

        // Inicializar el diccionario de manejadores de excepciones
        _exceptionHandlers = InitializeExceptionHandlers();

        // Configurar opciones de serialización JSON
        _jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = _environment.IsDevelopment(),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
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
            _logger.LogError(exception, "Error no manejado {CorrelationId}: {ExceptionType} - {ExceptionMessage}",
                correlationId, exception.GetType().Name, exception.Message);

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

            // Recolectar excepciones internas en una colección para mejor diagnóstico
            var innerExceptions = new List<string>();
            var currentException = exception.InnerException;
            while (currentException != null)
            {
                innerExceptions.Add($"{currentException.GetType().Name}: {currentException.Message}");
                currentException = currentException.InnerException;
            }

            if (innerExceptions.Any())
            {
                errorResponse.InnerErrors = innerExceptions;
            }
        }

        // Serializar y escribir la respuesta usando System.Text.Json
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, _jsonOptions));
    }

    private (HttpStatusCode StatusCode, string[] Messages) GetStatusCodeAndMessages(Exception exception)
    {
        // Buscar un manejador específico para este tipo de excepción
        foreach (var handler in _exceptionHandlers)
        {
            if (handler.Key.IsInstanceOfType(exception))
            {
                return handler.Value(exception);
            }
        }

        // Manejador por defecto para excepciones desconocidas
        return (HttpStatusCode.InternalServerError, new[] { "Ha ocurrido un error interno." });
    }

    private Dictionary<Type, Func<Exception, (HttpStatusCode, string[])>> InitializeExceptionHandlers()
    {
        return new Dictionary<Type, Func<Exception, (HttpStatusCode, string[])>>
        {
            [typeof(Ecommerce.Application.Exceptions.EntityNotFoundException)] = ex =>
                (HttpStatusCode.NotFound, new[] { ex.Message }),

            [typeof(FluentValidation.ValidationException)] = ex =>
            {
                var validationEx = (FluentValidation.ValidationException)ex;
                var errors = validationEx.Errors
                    .Select(e => e.ErrorMessage)
                    .ToArray();
                return (HttpStatusCode.BadRequest, errors);
            },

            [typeof(Ecommerce.Application.Exceptions.BadRequestException)] = ex =>
                (HttpStatusCode.BadRequest, new[] { ex.Message }),

            [typeof(DatabaseConnectionException)] = ex =>
                (HttpStatusCode.ServiceUnavailable, new[] { ex.Message }),

            [typeof(DataAccessException)] = ex =>
                (HttpStatusCode.InternalServerError, new[] { ex.Message }),

            [typeof(AuthenticationException)] = ex =>
                (HttpStatusCode.Unauthorized, new[] { ex.Message }),

            [typeof(TimeoutException)] = ex =>
                (HttpStatusCode.RequestTimeout, new[] { "La operación ha excedido el tiempo de espera." }),

            [typeof(UnauthorizedException)] = ex =>
                (HttpStatusCode.Forbidden, new[] { ex.Message }),

        };
    }
}