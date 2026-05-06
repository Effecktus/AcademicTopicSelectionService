using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.StudentApplications;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Сервис заявок студентов на темы ВКР.
/// Управление жизненным циклом заявки: создание, одобрение/отклонение, утверждение, отмена.
/// </summary>
public interface IStudentApplicationsService
{
    /// <summary>
    /// Получить список заявок, видимых пользователю по его роли.
    /// </summary>
    Task<PagedResult<StudentApplicationDto>> ListForRoleAsync(
        ListApplicationsQuery query, string roleCodeName, Guid userId, CancellationToken ct);

    /// <summary>
    /// Получить детальную заявку с историей действий.
    /// </summary>
    Task<StudentApplicationDetailDto?> GetDetailAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Создать заявку студентом.
    /// Проверяет: студент не подавал активных заявок, тема не занята, тема активна.
    /// </summary>
    Task<Result<StudentApplicationDto, ApplicationsError>> CreateAsync(
        CreateApplicationCommand command, Guid studentUserId, CancellationToken ct);

    /// <summary>
    /// Студент передаёт заявку научному руководителю: OnEditing → Pending (создаётся действие и уведомление).
    /// </summary>
    Task<Result<StudentApplicationDto, ApplicationsError>> SubmitToSupervisorAsync(
        Guid applicationId, Guid studentUserId, CancellationToken ct);

    /// <summary>
    /// Студент обновляет название и описание темы, связанной с заявкой (только OnEditing).
    /// </summary>
    Task<Result<StudentApplicationDto, ApplicationsError>> UpdateTopicAsync(
        Guid applicationId, UpdateApplicationTopicCommand command, Guid studentUserId, CancellationToken ct);

    /// <summary>
    /// Преподаватель одобряет заявку: Pending → ApprovedBySupervisor.
    /// Только научрук темы (Topics.CreatedBy == callerUserId).
    /// </summary>
    Task<Result<StudentApplicationDto, ApplicationsError>> ApproveBySupervisorAsync(
        Guid applicationId, ApproveBySupervisorCommand command, Guid callerUserId, CancellationToken ct);

    /// <summary>
    /// Преподаватель отклоняет заявку: Pending → RejectedBySupervisor.
    /// </summary>
    Task<Result<StudentApplicationDto, ApplicationsError>> RejectBySupervisorAsync(
        Guid applicationId, RejectBySupervisorCommand command, Guid callerUserId, CancellationToken ct);

    /// <summary>
    /// Преподаватель возвращает заявку на редактирование: Pending → OnEditing.
    /// </summary>
    Task<Result<StudentApplicationDto, ApplicationsError>> ReturnForEditingBySupervisorAsync(
        Guid applicationId, ReturnApplicationForEditingCommand command, Guid callerUserId, CancellationToken ct);

    /// <summary>
    /// Преподаватель передаёт заявку заведующему: ApprovedBySupervisor → PendingDepartmentHead.
    /// Только научрук темы. Если у научрука DepartmentId = null — ошибка.
    /// </summary>
    Task<Result<StudentApplicationDto, ApplicationsError>> SubmitToDepartmentHeadAsync(
        Guid applicationId, SubmitToDepartmentHeadCommand command, Guid callerUserId, CancellationToken ct);

    /// <summary>
    /// Заведующий утверждает заявку: PendingDepartmentHead → ApprovedByDepartmentHead.
    /// Проверяет: зав. кафедрой той же кафедры, что и научрук; лимит студентов.
    /// </summary>
    Task<Result<StudentApplicationDto, ApplicationsError>> ApproveByDepartmentHeadAsync(
        Guid applicationId, ApproveByDepartmentHeadCommand command, Guid callerUserId, CancellationToken ct);

    /// <summary>
    /// Заведующий отклоняет заявку: PendingDepartmentHead → RejectedByDepartmentHead.
    /// </summary>
    Task<Result<StudentApplicationDto, ApplicationsError>> RejectByDepartmentHeadAsync(
        Guid applicationId, RejectByDepartmentHeadCommand command, Guid callerUserId, CancellationToken ct);

    /// <summary>
    /// Заведующий возвращает заявку на редактирование: PendingDepartmentHead → OnEditing.
    /// </summary>
    Task<Result<StudentApplicationDto, ApplicationsError>> ReturnForEditingByDepartmentHeadAsync(
        Guid applicationId, ReturnApplicationForEditingCommand command, Guid callerUserId, CancellationToken ct);

    /// <summary>
    /// Студент отменяет заявку: Pending, ApprovedBySupervisor или OnEditing → Cancelled.
    /// Нельзя отменить после передачи заведующему.
    /// </summary>
    Task<Result<bool, ApplicationsError>> CancelAsync(
        Guid applicationId, Guid studentUserId, CancellationToken ct);
}
