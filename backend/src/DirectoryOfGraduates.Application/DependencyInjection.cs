using DirectoryOfGraduates.Application.Dictionaries.UserRoles;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryOfGraduates.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserRolesService, UserRolesService>();
        return services;
    }
}

