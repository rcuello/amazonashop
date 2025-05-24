using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
: IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators,ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        if (!_validators.Any())
        {
            _logger.LogDebug("No validators found for {RequestName}", requestName);
            return await next();
        }

        _logger.LogDebug("Validating {RequestName}", requestName);

        // Ejecutar todas las validaciones en paralelo
        var context = new ValidationContext<TRequest>(request);
        var validationTasks = _validators.Select(validator => validator.ValidateAsync(context, cancellationToken));

        var validationResults = await Task.WhenAll(validationTasks);

        // Recolectar todos los errores
        //var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        var failures = validationResults
            .Where(result => !result.IsValid)
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .ToList();

        if (failures.Any())
        {
            var errorMessages = failures.Select(f => f.ErrorMessage).ToList();

            _logger.LogWarning(
                "Validation failed for {RequestName}. Errors: {ValidationErrors}",
                requestName,
                string.Join("; ", errorMessages));

            throw new ValidationException(failures);
        }

        return await next();
    }
}