using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AcademicTopicSelectionService.API.Extensions;

/// <summary>
/// Доступ к стандартным claim из JWT (<c>sub</c>, роль).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Идентификатор пользователя из <c>NameIdentifier</c> или <c>sub</c>.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>
    /// Системное имя роли (<c>ClaimTypes.Role</c>).
    /// </summary>
    public static string? GetRoleCode(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role);
}
