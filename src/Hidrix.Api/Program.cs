using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DotNetEnv;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Hidrix.Api.Middleware;
using Hidrix.Api.Auth;
using Hidrix.Application;
using Hidrix.Infrastructure;
using Hidrix.Infrastructure.Identity;

// Carga opcional de ws-hidrix/.env (si existe) antes del host.
TryLoadDotEnv();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Hidrix.Api"));

    EnsureRequiredSecrets(builder.Configuration);

    builder.Services.AddMediator(options =>
    {
        options.ServiceLifetime = ServiceLifetime.Transient;
    });
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddSingleton<AuthCookieHelper>();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Hidrix API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Ingrese el token JWT. Ejemplo: Bearer {token}",
        });
        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        });
    });

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
    });

    var app = builder.Build();

    // Seed Identity roles + admin de arranque
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await SeedAdminUserAsync(scope.ServiceProvider, builder.Configuration);
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
            diagnosticContext.Set("UserName", httpContext.User.Identity?.Name ?? "anonymous");
        };
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La API Hidrix no pudo iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Busca y carga un archivo .env desde el cwd o la raíz de ws-hidrix.
static void TryLoadDotEnv()
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".env")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env")),
    };

    foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (!File.Exists(path))
        {
            continue;
        }

        Env.Load(path);
        return;
    }
}

// Valida secretos mínimos (User Secrets, .env o variables de entorno).
static void EnsureRequiredSecrets(IConfiguration configuration)
{
    var missing = new List<string>();

    if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
    {
        missing.Add("ConnectionStrings:DefaultConnection");
    }

    if (string.IsNullOrWhiteSpace(configuration["Jwt:Secret"]))
    {
        missing.Add("Jwt:Secret");
    }

    if (string.Equals(configuration["Visualiti:Enabled"], "true", StringComparison.OrdinalIgnoreCase)
        || configuration.GetValue("Visualiti:Enabled", false))
    {
        if (string.IsNullOrWhiteSpace(configuration["Visualiti:Usuario"]))
        {
            missing.Add("Visualiti:Usuario");
        }

        if (string.IsNullOrWhiteSpace(configuration["Visualiti:Password"]))
        {
            missing.Add("Visualiti:Password");
        }
    }

    if (missing.Count == 0)
    {
        return;
    }

    throw new InvalidOperationException(
        "Faltan secretos de configuración: " + string.Join(", ", missing) +
        ". Use User Secrets (Development), variables de entorno o copie ws-hidrix/.env.example → .env");
}

// Crea (o promueve a Admin) el usuario de arranque definido en Seed:AdminEmail / Seed:AdminPassword.
static async Task SeedAdminUserAsync(IServiceProvider services, IConfiguration configuration)
{
    var email = configuration["Seed:AdminEmail"];
    var password = configuration["Seed:AdminPassword"];
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return;
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var normalizedEmail = email.Trim().ToLowerInvariant();
    var user = await userManager.FindByEmailAsync(normalizedEmail);
    if (user is null)
    {
        user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            UsuaNombre = "Admin",
            UsuaActivo = true,
            UsuaFechaRegistro = DateTime.UtcNow,
            UsuaFechaCreacion = DateTime.UtcNow,
            UsuaFechaActualizacion = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            Log.Warning(
                "No se pudo crear el usuario admin sembrado {Email}: {Errors}",
                normalizedEmail,
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }
    }

    if (!await userManager.IsInRoleAsync(user, AppRoles.Admin))
    {
        await userManager.AddToRoleAsync(user, AppRoles.Admin);
    }
}

/// <summary>
/// Punto de entrada parcial para tests de integración.
/// </summary>
public partial class Program;
