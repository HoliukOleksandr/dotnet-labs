using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Common.Behaviors;

/// <summary>Біля обробника: вимірює час виконання (включно із запитами до MongoDB) і попереджає про повільні операції.</summary>
public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private const int SlowThresholdMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();
        if (sw.ElapsedMilliseconds > SlowThresholdMs)
            logger.LogWarning("Slow request {RequestName}: {Elapsed} ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);
        return response;
    }
}
