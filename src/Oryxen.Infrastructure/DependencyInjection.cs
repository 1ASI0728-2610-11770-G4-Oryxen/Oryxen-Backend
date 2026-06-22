using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Domain.Repositories;
using Oryxen.Application.Notifications;
using Oryxen.Infrastructure.External;
using Oryxen.Infrastructure.Persistence;
using Oryxen.Infrastructure.Persistence.Repositories;
using Oryxen.Infrastructure.Security;



namespace Oryxen.Infrastructure;

/// <summary>Registers persistence, repositories and security services into the DI container.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OryxenDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("OryxenDb"),
                npgsql => npgsql.MigrationsAssembly(typeof(OryxenDbContext).Assembly.FullName));
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OryxenDbContext>());

        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITelemetryRepository, TelemetryRepository>();
        services.AddScoped<IPlantRepository, PlantRepository>();
        services.AddScoped<IPlantDiagnosisRepository, PlantDiagnosisRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // ---- Notification Bounded Context ------------------------------------
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmailService, SendGridEmailService>();
        services.AddScoped<IPushNotificationService, FirebaseFcmService>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // ---- Third-party integration: OpenWeatherMap (typed HttpClient) ------
        services.Configure<OpenWeatherMapSettings>(configuration.GetSection(OpenWeatherMapSettings.SectionName));
        services.AddHttpClient<IWeatherService, OpenWeatherMapService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<OpenWeatherMapSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // ---- Third-party integration: Gemini Vision API (multimodal AI) ------
        services.Configure<GeminiVisionSettings>(configuration.GetSection(GeminiVisionSettings.SectionName));
        services.AddHttpClient<IMultimodalAiService, GeminiVisionService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<GeminiVisionSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ---- Third-party integration: Stripe (payment platform) -------------
        services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));
        services.AddHttpClient<IPaymentPlatformService, StripePaymentService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<StripeSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
