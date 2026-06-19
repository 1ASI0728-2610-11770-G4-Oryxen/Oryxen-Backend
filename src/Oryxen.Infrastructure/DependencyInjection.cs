using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Domain.Repositories;
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
            options.UseNpgsql(
                configuration.GetConnectionString("OryxenDb"),
                npgsql => npgsql.MigrationsAssembly(typeof(OryxenDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OryxenDbContext>());

        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITelemetryRepository, TelemetryRepository>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
