using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace DirectoryOfGraduates.API.Swagger;

/// <summary>
/// Конфигурация Swagger-документов по версиям API (v1, v2, ...).
/// </summary>
public sealed class ConfigureSwaggerOptions : IConfigureOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "DirectoryOfGraduates API",
                Version = description.ApiVersion.ToString(),
                Description = "Эталонная документация API. Используй этот стиль для остальных контроллеров."
            });
        }
    }
}

