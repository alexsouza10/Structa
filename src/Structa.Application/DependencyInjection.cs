using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Structa.Application.Common.Behaviors;

namespace Structa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddStructaApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
