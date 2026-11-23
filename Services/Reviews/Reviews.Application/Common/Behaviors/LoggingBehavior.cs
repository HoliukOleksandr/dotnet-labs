using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Common.Behaviors;

/// <summary>
/// Зовнішній behavior: логує кожну команду/запит. Якщо валідація відхилила запит, у лозі видно
/// «Handling ...» і далі «did not complete», але БЕЗ запису про виконання обробника.
/// Сам виняток логується один раз — у глобальному обробнику помилок.
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} {@Request}", name, request);
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName} in {Elapsed} ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning("{RequestName} did not complete after {Elapsed} ms: {ErrorType}",
                name, sw.ElapsedMilliseconds, ex.GetType().Name);
            throw;
        }
    }
}
