namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Абстракция для хеширования и верификации паролей.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Хеширует пароль.
    /// </summary>
    /// <param name="password">Пароль в открытом виде.</param>
    /// <returns>Хеш пароля.</returns>
    string Hash(string password);

    /// <summary>
    /// Проверяет соответствие пароля его хешу.
    /// </summary>
    /// <param name="password">Пароль в открытом виде.</param>
    /// <param name="hash">Ранее сохранённый хеш.</param>
    /// <returns><c>true</c>, если пароль совпадает с хешем.</returns>
    bool Verify(string password, string hash);
}
