using Microsoft.Extensions.DependencyInjection;
using Structa.Core.Messaging;
using Structa.Infrastructure.Messaging;

namespace Structa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStructaInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEventAggregator, EventAggregator>();

        return services;
    }
}
