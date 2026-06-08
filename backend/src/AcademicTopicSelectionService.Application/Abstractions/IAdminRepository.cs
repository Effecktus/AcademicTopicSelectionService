using AcademicTopicSelectionService.Application.Admin;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Агрегирующие запросы для панели администратора: аналитика и данные для экспорта.
/// </summary>
public interface IAdminRepository
{
    /// <summary>
    /// Возвращает сводную аналитику по заявкам и архиву ВКР.
    /// Если <paramref name="year"/> указан, все агрегаты фильтруются по этому году.
    /// </summary>
    Task<AdminAnalyticsDto> GetAnalyticsAsync(int? year, CancellationToken ct);

    /// <summary>
    /// Возвращает все записи архива ВКР для экспорта.
    /// </summary>
    Task<List<GwExportRow>> GetGwExportAsync(CancellationToken ct);

    /// <summary>
    /// Возвращает все заявки для экспорта.
    /// </summary>
    Task<List<ApplicationExportRow>> GetApplicationsExportAsync(CancellationToken ct);

    /// <summary>
    /// Возвращает всех пользователей для экспорта.
    /// </summary>
    Task<List<UserExportRow>> GetUsersExportAsync(CancellationToken ct);
}
