using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.API.Exceptions;
using AcademicTopicSelectionService.API.Health;
using AcademicTopicSelectionService.API.RateLimiting;
using AcademicTopicSelectionService.API.Swagger;
using AcademicTopicSelectionService.Application;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Infrastructure;
using AcademicTopicSelectionService.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

public sealed record HealthResponse(string Status, string Environment, DateTimeOffset Utc)
{
    public static HealthResponse Ok(string environment) => new("ok", environment, DateTimeOffset.UtcNow);
}

public sealed record HealthDbResponse(string Status, string Db, bool CanConnect);

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers(options =>
        {
            options.SuppressAsyncSuffixInActionNames = false;
        });
        builder.Services.AddEndpointsApiExplorer();

        builder.Services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1.0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        builder.Services.AddSwaggerGen(options =>
        {
            // Убираем коллизии одинаковых имён DTO из разных namespace.
            options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

            var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Введите JWT access-токен (без префикса Bearer)."
            });

            options.OperationFilter<SecurityRequirementsOperationFilter>();
        });
        builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        var relaxedRateLimit = builder.Environment.EnvironmentName == "Testing";
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicyNames.AuthLogin, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = relaxedRateLimit ? 50_000 : 15,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            options.AddPolicy(RateLimitPolicyNames.AuthRefresh, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = relaxedRateLimit ? 50_000 : 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        // JWT аутентификация
        var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
        var secretKey = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("Missing Jwt:SecretKey configuration");

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
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddApplicationAuthorization();

        var corsOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>())
            .Where(static o => !string.IsNullOrWhiteSpace(o))
            .Select(static o => o.TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (corsOrigins.Length > 0)
        {
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(corsOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        description.GroupName.ToUpperInvariant());
                }
            });
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (corsOrigins.Length > 0)
            app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription().AllowAnonymous();

        app.MapGet("/health", () => Results.Ok(HealthResponse.Ok(app.Environment.EnvironmentName)))
            .WithName("Health")
            .WithTags("Health")
            .WithSummary("Проверка доступности API.")
            .WithDescription("Smoke-check: подтверждает, что процесс API запущен и отвечает. Не проверяет БД.")
            .Produces<HealthResponse>(StatusCodes.Status200OK)
            .WithOpenApi()
            .AllowAnonymous();

        app.MapGet("/health/db", async (HttpContext httpContext, IConfiguration configuration, IDatabaseHealthChecker checker, CancellationToken ct) =>
            {
                if (!HealthDbAccess.IsAuthorized(httpContext, configuration))
                    return Results.Unauthorized();

                var canConnect = await checker.CanConnectAsync(ct);
                return Results.Ok(new HealthDbResponse(
                    Status: canConnect ? "ok" : "failed",
                    Db: "postgres",
                    CanConnect: canConnect));
            })
            .WithName("HealthDb")
            .WithTags("Health")
            .WithSummary("Проверка доступности PostgreSQL из API.")
            .WithDescription(
                "Требуется JWT администратора или заголовок X-Health-Probe-Key, если задан ключ Health:DbProbeKey в конфигурации.")
            .Produces<HealthDbResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithOpenApi()
            .AllowAnonymous();

        app.MapControllers();

        app.Run();
    }
}
