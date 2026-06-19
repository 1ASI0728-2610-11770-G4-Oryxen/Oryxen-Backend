using Microsoft.Extensions.DependencyInjection;
using Oryxen.Application.Auth;
using Oryxen.Application.Telemetry;

namespace Oryxen.Application;

/// <summary>Registers the Application-layer services into the DI container.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITelemetryService, TelemetryService>();

        return services;
    }
}
