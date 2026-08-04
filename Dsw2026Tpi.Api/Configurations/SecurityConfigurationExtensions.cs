using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.RateLimiting;

namespace Dsw2026Tpi.Api.Configurations;

public static class SecurityConfigurationExtensions
{
    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        //Obtener parámetros para creación del JWT desde appsettings.json
        var jwtConfig = configuration.GetSection("Jwt");
        var keyText = jwtConfig["Key"] ?? throw new ArgumentNullException("JWT Key");
        var issuer = jwtConfig["Issuer"] ?? throw new ArgumentNullException("JWT Issuer");
        var audience = jwtConfig["Audience"] ?? throw new ArgumentNullException("JWT Audience");
        var key = Encoding.UTF8.GetBytes(keyText);

        //Agregar autenticación
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                //Definir parámetros para la generación del token
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminPolicy, policy =>
                policy.RequireRole(Roles.Administrator))
            .AddPolicy(Policies.PatientPolicy, policy =>
                policy.RequireRole(Roles.Patient))
            .AddPolicy(Policies.AdminOrPatientPoliciy, policy =>
                policy.RequireRole(Roles.Administrator, Roles.Patient));
        return services;
    }

    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration)
    {
        //Obtener configuración para CORS desde appsettings.json
        var allowedOrigins = configuration
                            .GetSection("Cors:AllowedOrigins")
                            .Get<string[]>()?
                            .Where(origin => !string.IsNullOrWhiteSpace(origin))
                            .Select(origin => origin.TrimEnd('/'))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray();

        //Si no se definió configuración en el archivo, utilizar la que se define
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            allowedOrigins =
            [
                "http://localhost",
                "https://localhost"
            ];
        }

        //Agregar CORS con la política por defecto a partir de las URLs definidas
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddAppIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password = new PasswordOptions
            {
                RequiredLength = 6,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true
            };

        }).AddRoles<IdentityRole>()
          .AddEntityFrameworkStores<AuthenticationDbContext>()
          .AddSignInManager()
          .AddDefaultTokenProviders();
        return services;
    }

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var clientKey = context.HttpContext.User.Identity?.IsAuthenticated == true
                    ? context.HttpContext.User.Identity.Name
                    : context.HttpContext.Connection.RemoteIpAddress?.ToString();

                logger.LogWarning($"Rate limit excedido para: {clientKey} en el path: {context.HttpContext.Request.Path}");

                var errorResponse = new
                {
                    Code = "TOO_MANY_REQUESTS",
                    Message = "Ha superado el limite de solicitudes permitidas. Intente nuevamente más tarde."
                };
                await context.HttpContext.Response.WriteAsJsonAsync(errorResponse);
            };

                var adminConfig = configuration.GetSection("RateLimiting:AdminAuth");
                options.AddPolicy("AdminAuthPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unkwown_ip",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = adminConfig.GetValue<int>("PermitLimit"),
                            Window = TimeSpan.FromMinutes(adminConfig.GetValue<int>("WindowInMinutes")),
                            QueueLimit = 0
                        }));

                var patientConfig = configuration.GetSection("RateLimiting:PatientAuth");
                options.AddPolicy("PatientAuthPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unkwown_ip",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = patientConfig.GetValue<int>("PermitLimit"),
                            Window = TimeSpan.FromMinutes(patientConfig.GetValue<int>("WindowInMinutes")),
                            QueueLimit = 0
                        }));

                var bookingConfig = configuration.GetSection("RateLimiting:AppointmentBooking");
                options.AddPolicy("AppointmentBookingPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.Identity?.Name
                                   ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                   ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                   ?? "unknown_user",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = bookingConfig.GetValue<int>("PermitLimit"),
                            Window = TimeSpan.FromMinutes(bookingConfig.GetValue<int>("WindowInMinutes")),
                            QueueLimit = 0
                        }));

                var generalConfig = configuration.GetSection("RateLimiting:General");
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                        ? httpContext.User.Identity.Name!
                        : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = generalConfig.GetValue<int>("PermitLimit"),
                        Window = TimeSpan.FromMinutes(generalConfig.GetValue<int>("WindowInMinutes")),
                        QueueLimit = 0
                    });
                });

            
        });

        return services;
    }

}