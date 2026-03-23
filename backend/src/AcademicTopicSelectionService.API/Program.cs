using System.Reflection;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AcademicTopicSelectionService.API.Swagger;
using AcademicTopicSelectionService.Application;
using AcademicTopicSelectionService.Infrastructure;
using AcademicTopicSelectionService.Infrastructure.Data;

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
            var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
        builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

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

        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

        app.MapGet("/health", () => Results.Ok(HealthResponse.Ok(app.Environment.EnvironmentName)))
            .WithName("Health")
            .WithTags("Health")
            .WithSummary("Проверка доступности API.")
            .WithDescription("Smoke-check: подтверждает, что процесс API запущен и отвечает. Не проверяет БД.")
            .Produces<HealthResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        app.MapGet("/health/db", async (CancellationToken ct) =>
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var canConnect = await db.Database.CanConnectAsync(ct);
            return Results.Ok(new HealthDbResponse(
                Status: canConnect ? "ok" : "failed",
                Db: "postgres",
                CanConnect: canConnect));
        })
        .WithName("HealthDb")
        .WithTags("Health")
        .WithSummary("Проверка доступности PostgreSQL из API.")
        .WithDescription("Проверяет, что API может подключиться к PostgreSQL (Database.CanConnectAsync).")
        .Produces<HealthDbResponse>(StatusCodes.Status200OK)
        .WithOpenApi();

        app.MapControllers();

        app.Run();
    }
}
