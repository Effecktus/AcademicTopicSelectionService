using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AcademicTopicSelectionService.IntegrationTests.Infrastructure;

/// <summary>
/// Выпуск тестовых access-токенов с теми же issuer/audience/ключом, что и <see cref="TestWebApplicationFactory"/>.
/// </summary>
public static class IntegrationTestJwt
{
    /// <summary>Создаёт JWT с ролью (как в <c>JwtTokenGenerator</c>).</summary>
    public static string CreateAccessToken(string role, Guid? userId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, uid.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "integration-test@local"),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestWebApplicationFactory.TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestWebApplicationFactory.TestJwtIssuer,
            audience: TestWebApplicationFactory.TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
