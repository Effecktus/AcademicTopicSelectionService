using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.ApplicationActions;
using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicDegrees;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicTitles;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationStatuses;
using AcademicTopicSelectionService.Application.Dictionaries.NotificationTypes;
using AcademicTopicSelectionService.Application.Dictionaries.Positions;
using AcademicTopicSelectionService.Application.Dictionaries.StudyGroups;
using AcademicTopicSelectionService.Application.Dictionaries.TopicCreatorTypes;
using AcademicTopicSelectionService.Application.Dictionaries.TopicStatuses;
using AcademicTopicSelectionService.Application.Dictionaries.UserRoles;
using AcademicTopicSelectionService.Application.Students;
using AcademicTopicSelectionService.Application.StudentApplications;
using AcademicTopicSelectionService.Application.SupervisorRequests;
using AcademicTopicSelectionService.Application.Teachers;
using AcademicTopicSelectionService.Application.Topics;
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
        // Аутентификация
        services.AddScoped<IAuthService, AuthService>();

        // Бизнес-сущности
        services.AddScoped<IApplicationActionsService, ApplicationActionsService>();

        // Справочники
        services.AddScoped<IUserRolesService, UserRolesService>();
        services.AddScoped<IApplicationStatusesService, ApplicationStatusesService>();
        services.AddScoped<IApplicationActionStatusesService, ApplicationActionStatusesService>();
        services.AddScoped<ITopicStatusesService, TopicStatusesService>();
        services.AddScoped<ITopicCreatorTypesService, TopicCreatorTypesService>();
        services.AddScoped<INotificationTypesService, NotificationTypesService>();
        services.AddScoped<IAcademicDegreesService, AcademicDegreesService>();
        services.AddScoped<IAcademicTitlesService, AcademicTitlesService>();
        services.AddScoped<IPositionsService, PositionsService>();
        services.AddScoped<IStudyGroupsService, StudyGroupsService>();

        services.AddScoped<ITeachersService, TeachersService>();
        services.AddScoped<ITopicsService, TopicsService>();
        services.AddScoped<IStudentsService, StudentsService>();

        // Заявки
        services.AddScoped<IStudentApplicationsService, StudentApplicationsService>();
        services.AddScoped<ISupervisorRequestsService, SupervisorRequestsService>();

        return services;
    }
}

