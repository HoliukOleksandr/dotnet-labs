using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Common.Behaviors;

namespace Reviews.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = config["LuckyPenny:LicenseKey"];
            cfg.RegisterServicesFromAssembly(assembly);               // обробники — автоматично
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));          // 1 — зовнішній
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));       // 2
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));      // 3 — біля обробника
        });

        services.AddValidatorsFromAssembly(assembly);                 // усі AbstractValidator<T>
        return services;
    }
}
