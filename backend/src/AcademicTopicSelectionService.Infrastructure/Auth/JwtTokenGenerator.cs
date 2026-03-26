using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AcademicTopicSelectionService.Infrastructure.Auth;

/// <summary>
/// Генератор JWT access-токенов на основе HMAC-SHA256.
/// Требует загруженную навигацию <c>User.Role</c>.
/// </summary>
public sealed class JwtTokenGenerator(IOptions<JwtSettings> options) : IJwtTokenGenerator
{
    private readonly JwtSettings _settings = options.Value;

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.CodeName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshTokenValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    /// <inheritdoc />
    public DateTime GetRefreshTokenExpiration() =>
        DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays);
}
