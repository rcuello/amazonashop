using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ecommerce.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var requestName = typeof(TRequest).Name;

        // Log si toma más de 500ms
        if (elapsedMs > 500)
        {
            _logger.LogWarning("Long running request: {RequestName} took {ElapsedMs}ms {@Request}",
                requestName, elapsedMs, request);
        }

        return response;
    }
}
