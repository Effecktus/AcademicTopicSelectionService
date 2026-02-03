using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using DirectoryOfGraduates.API.Swagger;
using DirectoryOfGraduates.Application;
using DirectoryOfGraduates.Infrastructure;
using DirectoryOfGraduates.Infrastructure.Data;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1.0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddSwaggerGen(options =>
{
    // XML comments (summary/param/response) → Swagger
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
    });
}

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(
    HealthResponse.Ok(app.Environment.EnvironmentName)))
.WithName("Health")
.WithTags("Health")
.WithSummary("Проверка доступности API.")
.WithDescription("Smoke-check: подтверждает, что процесс API запущен и отвечает. Не проверяет БД.")
.Produces<HealthResponse>(StatusCodes.Status200OK)
.WithOpenApi();

app.MapGet("/health/db", async (IConfiguration config, CancellationToken ct) =>
{
    // Explicit DI scope via Minimal endpoint:
    // if db is reachable -> ok
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

public sealed record HealthResponse(string Status, string Environment, DateTimeOffset Utc)
{
    public static HealthResponse Ok(string environment) => new("ok", environment, DateTimeOffset.UtcNow);
}

public sealed record HealthDbResponse(string Status, string Db, bool CanConnect);
