using AcademicTopicSelectionService.Application.Dictionaries.AcademicDegrees;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicTitles;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationStatuses;
using AcademicTopicSelectionService.Application.Dictionaries.NotificationTypes;
using AcademicTopicSelectionService.Application.Dictionaries.Positions;
using AcademicTopicSelectionService.Application.Dictionaries.TopicStatuses;
using AcademicTopicSelectionService.Application.Dictionaries.UserRoles;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.Application;

/// <summary>
/// Методы расширения для регистрации сервисов слоя Application в DI-контейнере.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует все сервисы бизнес-логики (Application layer) в контейнере зависимостей.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Справочники
        services.AddScoped<IUserRolesService, UserRolesService>();
        services.AddScoped<IApplicationStatusesService, ApplicationStatusesService>();
        services.AddScoped<ITopicStatusesService, TopicStatusesService>();
        services.AddScoped<INotificationTypesService, NotificationTypesService>();
        services.AddScoped<IAcademicDegreesService, AcademicDegreesService>();
        services.AddScoped<IAcademicTitlesService, AcademicTitlesService>();
        services.AddScoped<IPositionsService, PositionsService>();

        return services;
    }
}

