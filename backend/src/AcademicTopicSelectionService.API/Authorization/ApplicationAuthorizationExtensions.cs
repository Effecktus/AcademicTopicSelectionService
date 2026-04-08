using Microsoft.AspNetCore.Authorization;

namespace AcademicTopicSelectionService.API.Authorization;

/// <summary>
/// Регистрация политик авторизации: по умолчанию все конечные точки требуют аутентификации JWT.
/// </summary>
public static class ApplicationAuthorizationExtensions
{
    /// <summary>
    /// Настраивает fallback-политику: неанонимные конечные точки доступны только с валидным Bearer-токеном.
    /// </summary>
    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
