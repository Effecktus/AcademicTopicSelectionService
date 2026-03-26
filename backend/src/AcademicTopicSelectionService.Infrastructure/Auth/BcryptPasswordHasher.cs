using AcademicTopicSelectionService.Application.Abstractions;

namespace AcademicTopicSelectionService.Infrastructure.Auth;

/// <summary>
/// Реализация хешера паролей на основе BCrypt (cost factor 12 по умолчанию).
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    /// <inheritdoc />
    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Хеш не является валидным BCrypt-хешем — считаем пароль неверным
            return false;
        }
    }
}
