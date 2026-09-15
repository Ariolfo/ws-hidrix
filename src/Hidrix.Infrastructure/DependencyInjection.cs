using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Infrastructure.Auth;
using Hidrix.Infrastructure.Identity;
using Hidrix.Infrastructure.Options;
using Hidrix.Infrastructure.Persistence;
using Hidrix.Infrastructure.Persistence.Repositories;
using Hidrix.Infrastructure.Services;

namespace Hidrix.Infrastructure;

/// <summary>
/// Registro de dependencias de Infrastructure.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega EF Core, Identity, JWT, HttpClient Visualiti y servicios de infraestructura.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<VisualitiOptions>(configuration.GetSection(VisualitiOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<HealthOptions>(configuration.GetSection(HealthOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurada");

        services.AddDbContext<HidrixDbContext>(options =>
            options.UseSqlServer(connectionString));

        // ASP.NET Core Identity (sin cookies — API pura con JWT)
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<HidrixDbContext>()
            .AddDefaultTokenProviders()
            .AddPasswordValidator<CommonPasswordValidator>();

        // JWT como esquema de autenticación principal
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret debe configurarse con al menos 32 caracteres (User Secrets, .env o variables de entorno).");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            context.Token = context.Request.Cookies[AuthCookieNames.Access];
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();

        // Repositories and application services
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IGeoRepository, GeoRepository>();
        services.AddScoped<ISensorCatalogService, SensorCatalogService>();
        services.AddScoped<ICropCatalogService, CropCatalogService>();
        services.AddScoped<IIrrigationCalculationService, IrrigationCalculationService>();
        services.AddScoped<IIrrigationEventNoteService, IrrigationEventNoteService>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IGeoResolver, GeoResolver>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<IAuthSettings, AuthSettings>();
        services.AddSingleton<IVisualitiMoistureCache, VisualitiMoistureCache>();
        services.AddScoped<IVisualitiStationEnricher, VisualitiStationEnricher>();

        var visualiti = configuration.GetSection(VisualitiOptions.SectionName).Get<VisualitiOptions>()
                        ?? new VisualitiOptions();

        services.AddHttpClient<IVisualitiClient, VisualitiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (environment.IsDevelopment() && !visualiti.SslVerify)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        static (HttpRequestMessage _, X509Certificate2? _, X509Chain? _, SslPolicyErrors _) => true;
                }

                return handler;
            });

        return services;
    }
}
