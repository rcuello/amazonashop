using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Exceptions.App;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Ecommerce.Application.Behaviors;

public class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TRequest> _logger;

    public UnhandledExceptionBehavior(ILogger<TRequest> logger)
    {
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch(SqlException sqlEx)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogError(sqlEx, "SQL Exception for request {Name} {@Request}: {Message}", requestName, request, sqlEx.Message);
            
            if (sqlEx.Number == -1 || sqlEx.Number == 2 || sqlEx.Number == 53 || sqlEx.Number == 40) // Connection errors
            {
                throw new DatabaseConnectionException(
                    "No se pudo establecer conexión con la base de datos. Por favor, intente más tarde.", sqlEx);
            }

            throw new DataAccessException(
                    "Ocurrió un error al acceder a los datos. Por favor, intente más tarde.", sqlEx);
        }
        catch(DbException dbEx)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogError(dbEx, "Database Exception for request {Name} {@Request}: {Message}", requestName, request, dbEx.Message);
            throw new DataAccessException(
                    "Ocurrió un error en la operación de base de datos. Por favor, intente más tarde.", dbEx);
        }
        catch (ApplicationExceptionBase)
        {
            // Re-lanzar sin modificar para que llegue al middleware con su StatusCode
            throw;
        }        
        catch (FluentValidation.ValidationException)
        {
            // Re-lanzar sin modificar para que llegue al middleware
            throw;
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogError(ex, "Application Exception for request {Name} {@Request}: {Message}", requestName, request, ex.Message);
            throw new Exceptions.App.ApplicationException("Ocurrió un error procesando su solicitud.", ex);
        }




    }
}