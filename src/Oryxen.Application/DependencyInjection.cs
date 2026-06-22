using Microsoft.Extensions.DependencyInjection;
using Oryxen.Application.AI;
using Oryxen.Application.Auth;
using Oryxen.Application.Billing;
using Oryxen.Application.Plants;
using Oryxen.Application.Telemetry;

namespace Oryxen.Application;

/// <summary>Registers the Application-layer services into the DI container.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITelemetryService, TelemetryService>();
        services.AddScoped<IPlantService, PlantService>();
        services.AddScoped<IDiagnosisService, DiagnosisService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        return services;
    }
}
