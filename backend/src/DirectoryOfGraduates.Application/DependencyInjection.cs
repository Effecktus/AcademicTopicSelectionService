using DirectoryOfGraduates.Application.Dictionaries.ApplicationStatuses;
using DirectoryOfGraduates.Application.Dictionaries.UserRoles;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryOfGraduates.Application;

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
        
        return services;
    }
}

