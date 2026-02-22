using Longa.Application.Common.Interfaces;
using Longa.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Longa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        return services;
    }
}
