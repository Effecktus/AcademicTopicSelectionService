using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.StudentApplications;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий заявок студентов: чтение и запись.
/// </summary>
public interface IStudentApplicationsRepository
{
    /// <summary>
    /// Получить список заявок, видимых пользователю с указанной ролью.
    /// </summary>
    Task<PagedResult<StudentApplicationDto>> ListForRoleAsync(
        ListApplicationsQuery query, string roleCodeName, Guid userId, CancellationToken ct);

    /// <summary>
    /// Получить детальную заявку с историей действий.
    /// </summary>
    Task<StudentApplicationDetailDto?> GetDetailAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Получить сущность заявки с навигацией (для изменения).
    /// </summary>
    Task<StudentApplication?> GetByIdWithTrackingAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Добавить заявку.
    /// </summary>
    Task<StudentApplication> AddAsync(StudentApplication application, CancellationToken ct);

    /// <summary>
    /// Сохранить изменения tracked-сущности.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Добавить запись истории изменения темы заявки (будет сохранена при следующем вызове SaveChanges у контекста БД).
    /// </summary>
    void StageApplicationTopicChangeHistory(ApplicationTopicChangeHistory entry);

    /// <summary>
    /// Проверить существование активной заявки на тему (для проверки конкурентности).
    /// Активная = не в терминальном статусе.
    /// </summary>
    Task<bool> HasActiveApplicationOnTopicAsync(Guid topicId, CancellationToken ct);

    /// <summary>
    /// Проверить, есть ли у студента уже заявка (любая не-терминальная).
    /// </summary>
    Task<bool> StudentHasActiveApplicationAsync(Guid studentId, CancellationToken ct);

    /// <summary>
    /// Посчитать количество заявок научрука, занятых слотов.
    /// В лимит включаются только заявки, прошедшие финальное утверждение заведующим кафедрой
    /// (статус ApprovedByDepartmentHead) для соответствующего научрука.
    /// </summary>
    Task<int> CountOccupiedSlotsBySupervisorAsync(Guid supervisorUserId, CancellationToken ct);

    /// <summary>
    /// Найти Student.Id по Users.Id.
    /// </summary>
    Task<Guid?> GetStudentIdByUserIdAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Найти Teacher по Users.Id (для проверки лимита студентов).
    /// </summary>
    Task<Teacher?> GetTeacherByUserIdAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Найти одобренный запрос на научного руководителя конкретного студента.
    /// </summary>
    Task<SupervisorRequest?> GetApprovedSupervisorRequestAsync(Guid supervisorRequestId, Guid studentId, CancellationToken ct);

    /// <summary>
    /// Участники чата по заявке и статус связанного <see cref="SupervisorRequest"/> (если есть).
    /// </summary>
    Task<ApplicationChatAccessInfo?> GetChatAccessAsync(Guid applicationId, CancellationToken ct);
}
