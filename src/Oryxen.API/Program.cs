using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Oryxen.API.Middleware;
using Oryxen.Application;
using Oryxen.Infrastructure;
using Oryxen.Infrastructure.Persistence;
using Oryxen.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// ---- Service registration ----------------------------------------------------
builder.Services.AddControllers();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ---- CORS: explicit allow-list from configuration (never AllowAnyOrigin) -----
const string CorsPolicyName = "OryxenCors";

var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

// Vite dev/preview servers + Live Server for the landing page. Only outside a deployed
// environment: they would otherwise let any page served from a developer machine call the
// deployed API with a real user's session.
var developmentOrigins = builder.Environment.IsDevelopment()
    ? new[]
    {
        "http://localhost:5173",
        "http://localhost:4173",
        "http://localhost:5500",
        "http://127.0.0.1:5500"
    }
    : [];

// Deployed frontends, always allowed so a redeploy works without extra configuration.
// The API's own origin (https://oryxen-backend.onrender.com) is deliberately absent:
// it is the server, not a browser client.
var productionOrigins = new[]
{
    "https://oryxen-web-application.vercel.app",
    "https://oryxen-landing.web.app",
    "https://oryxen-landing.firebaseapp.com"
};

var allowedOrigins = configuredOrigins
    .Concat(developmentOrigins)
    .Concat(productionOrigins)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Where(origin => builder.Environment.IsDevelopment() || !IsLoopback(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

static bool IsLoopback(string origin) =>
    Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Oryxen API",
        Version = "v1",
        Description = "RESTful API for the Oryxen smart plant care platform — Auth & Identity and IoT telemetry ingestion."
    });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token (without the 'Bearer ' prefix).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [jwtScheme] = Array.Empty<string>()
    });
});

var app = builder.Build();

app.Logger.LogInformation("CORS allow-list ({Count}): {Origins}", allowedOrigins.Length, string.Join(", ", allowedOrigins));

// ---- Database migration on startup (dev convenience) -------------------------
await ApplyMigrationsAsync(app);
await SeedPlansAsync(app);

// ---- HTTP pipeline -----------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger is dev-only by default, but can be force-enabled for staging demos via "Swagger:Enabled".
var swaggerEnabled = builder.Configuration.GetValue<bool?>("Swagger:Enabled") ?? app.Environment.IsDevelopment();
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Oryxen API v1");
        options.DocumentTitle = "Oryxen API — Swagger";
    });
}

app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<OryxenDbContext>();
        await db.Database.MigrateAsync();
        logger.LogInformation("Database is up to date (migrations applied).");
    }
    catch (Exception ex)
    {
        logger.LogWarning(
            ex,
            "Could not apply database migrations on startup. Make sure PostgreSQL is running (docker compose up -d). The API will still start so Swagger remains reachable.");
    }
}

/// <summary>
/// Idempotent seed of the commercial plan catalog (Basic / Premium, mirroring the Landing
/// Page pricing). Runs after migrations so GET /api/v1/plans is demonstrable on a fresh
/// database without manual INSERTs; existing rows are never duplicated or overwritten.
/// </summary>
static async Task SeedPlansAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<OryxenDbContext>();
        if (await db.Plans.AnyAsync())
        {
            return;
        }

        db.Plans.AddRange(
            new Oryxen.Domain.Entities.Plan
            {
                Name = "Basic",
                Price = 25m,
                Currency = "PEN",
                BillingCycleMonths = 1,
                Features = "3 plants monitor,Basic watering,Chatbot AI basic functions,Email support",
                IsActive = true
            },
            new Oryxen.Domain.Entities.Plan
            {
                Name = "Premium",
                Price = 50m,
                Currency = "PEN",
                BillingCycleMonths = 1,
                Features = "Unlimited plant monitors,Advanced watering,Priority support,Chatbot AI premium functions,Community access",
                IsActive = true
            });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded the plan catalog (Basic S/.25, Premium S/.50).");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not seed the plan catalog; GET /api/v1/plans may be empty until the database is reachable.");
    }
}
