namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Абстракция для проверки доступности базы данных.
/// </summary>
public interface IDatabaseHealthChecker
{
    /// <summary>
    /// Проверяет возможность подключения к базе данных.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если подключение установлено; <c>false</c> в противном случае.</returns>
    Task<bool> CanConnectAsync(CancellationToken ct);
}
