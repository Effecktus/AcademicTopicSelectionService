using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.SupervisorRequests;

/// <summary>
/// Бизнес-логика управления запросами на выбор научного руководителя.
/// </summary>
public sealed class SupervisorRequestsService(
    ISupervisorRequestsRepository repository,
    IUsersRepository usersRepository,
    IApplicationStatusesRepository applicationStatusesRepository) : ISupervisorRequestsService
{
    private const string PendingStatus = "Pending";
    private const string ApprovedStatus = "ApprovedBySupervisor";
    private const string RejectedStatus = "RejectedBySupervisor";
    private const string CancelledStatus = "Cancelled";

    /// <inheritdoc />
    public Task<PagedResult<SupervisorRequestDto>> ListForRoleAsync(
        ListSupervisorRequestsQuery query,
        string roleCodeName,
        Guid userId,
        CancellationToken ct)
        => repository.ListForRoleAsync(query, roleCodeName, userId, ct);

    /// <inheritdoc />
    public Task<SupervisorRequestDetailDto?> GetDetailAsync(Guid id, CancellationToken ct)
        => repository.GetDetailAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<SupervisorRequestDto, SupervisorRequestsError>> CreateAsync(
        CreateSupervisorRequestCommand command,
        Guid studentUserId,
        CancellationToken ct)
    {
        if (command.TeacherUserId == Guid.Empty)
            return Fail(SupervisorRequestsError.Validation, "TeacherUserId is required");

        var studentUser = await usersRepository.GetByIdAsync(studentUserId, ct);
        if (studentUser is null)
            return Fail(SupervisorRequestsError.NotFound, "User not found");

        if (!string.Equals(studentUser.Role.CodeName, "Student", StringComparison.Ordinal))
            return Fail(SupervisorRequestsError.Forbidden, "Only students can create supervisor requests");

        var teacherUser = await usersRepository.GetByIdAsync(command.TeacherUserId, ct);
        if (teacherUser is null)
            return Fail(SupervisorRequestsError.NotFound, "Teacher user not found");

        if (!string.Equals(teacherUser.Role.CodeName, "Teacher", StringComparison.Ordinal))
            return Fail(SupervisorRequestsError.Validation, "Selected user is not a teacher");

        var studentId = await repository.GetStudentIdByUserIdAsync(studentUserId, ct);
        if (studentId is null)
            return Fail(SupervisorRequestsError.Validation, "Student profile not found");

        if (await repository.HasActiveRequestForTeacherAsync(studentId.Value, command.TeacherUserId, ct))
            return Fail(SupervisorRequestsError.Conflict, "Active request for this teacher already exists");

        if (studentUser.DepartmentId is Guid departmentId)
        {
            var activeRequests = await repository.CountActiveRequestsForStudentAsync(studentId.Value, ct);
            var teachersInDepartment = await repository.CountTeachersInDepartmentAsync(departmentId, ct);
            if (teachersInDepartment > 0 && activeRequests >= teachersInDepartment)
                return Fail(
                    SupervisorRequestsError.Conflict,
                    "Active requests limit for your department has been reached");
        }

        var pendingStatusId = await applicationStatusesRepository.GetIdByCodeNameAsync(PendingStatus, ct);
        if (pendingStatusId is null)
            return Fail(SupervisorRequestsError.Validation, "Status 'Pending' not found");

        var entity = new SupervisorRequest
        {
            StudentId = studentId.Value,
            TeacherUserId = command.TeacherUserId,
            StatusId = pendingStatusId.Value,
            Comment = string.IsNullOrWhiteSpace(command.Comment) ? null : command.Comment.Trim()
        };

        var created = await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);

        var dto = await repository.GetDetailAsync(created.Id, ct);
        if (dto is null)
            return Fail(SupervisorRequestsError.NotFound, "Supervisor request created but not found");

        return Result<SupervisorRequestDto, SupervisorRequestsError>.Ok(ToDto(dto));
    }

    /// <inheritdoc />
    public async Task<Result<SupervisorRequestDto, SupervisorRequestsError>> ApproveAsync(
        Guid id,
        Guid teacherUserId,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdWithTrackingAsync(id, ct);
        if (entity is null)
            return Fail(SupervisorRequestsError.NotFound, "Supervisor request not found");

        if (entity.TeacherUserId != teacherUserId)
            return Fail(SupervisorRequestsError.Forbidden, "Only requested teacher can approve this request");

        var currentStatus = entity.Status.CodeName;
        if (!string.Equals(currentStatus, PendingStatus, StringComparison.Ordinal))
            return Fail(
                SupervisorRequestsError.InvalidTransition,
                $"Cannot approve request from status '{currentStatus}'");

        var approvedStatusId = await applicationStatusesRepository.GetIdByCodeNameAsync(ApprovedStatus, ct);
        if (approvedStatusId is null)
            return Fail(SupervisorRequestsError.Validation, "Status 'ApprovedBySupervisor' not found");

        entity.StatusId = approvedStatusId.Value;
        entity.Comment ??= "Approved by supervisor";

        await repository.CancelAllActiveRequestsExceptAsync(entity.StudentId, entity.Id, ct);
        await repository.SaveChangesAsync(ct);

        var dto = await repository.GetDetailAsync(id, ct);
        if (dto is null)
            return Fail(SupervisorRequestsError.NotFound, "Supervisor request not found after update");

        return Result<SupervisorRequestDto, SupervisorRequestsError>.Ok(ToDto(dto));
    }

    /// <inheritdoc />
    public async Task<Result<SupervisorRequestDto, SupervisorRequestsError>> RejectAsync(
        Guid id,
        RejectSupervisorRequestCommand command,
        Guid teacherUserId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Comment))
            return Fail(SupervisorRequestsError.Validation, "Comment is required for rejection");

        var entity = await repository.GetByIdWithTrackingAsync(id, ct);
        if (entity is null)
            return Fail(SupervisorRequestsError.NotFound, "Supervisor request not found");

        if (entity.TeacherUserId != teacherUserId)
            return Fail(SupervisorRequestsError.Forbidden, "Only requested teacher can reject this request");

        var currentStatus = entity.Status.CodeName;
        if (!string.Equals(currentStatus, PendingStatus, StringComparison.Ordinal))
            return Fail(
                SupervisorRequestsError.InvalidTransition,
                $"Cannot reject request from status '{currentStatus}'");

        var rejectedStatusId = await applicationStatusesRepository.GetIdByCodeNameAsync(RejectedStatus, ct);
        if (rejectedStatusId is null)
            return Fail(SupervisorRequestsError.Validation, "Status 'RejectedBySupervisor' not found");

        entity.StatusId = rejectedStatusId.Value;
        entity.Comment = command.Comment.Trim();

        await repository.SaveChangesAsync(ct);

        var dto = await repository.GetDetailAsync(id, ct);
        if (dto is null)
            return Fail(SupervisorRequestsError.NotFound, "Supervisor request not found after update");

        return Result<SupervisorRequestDto, SupervisorRequestsError>.Ok(ToDto(dto));
    }

    /// <inheritdoc />
    public async Task<Result<bool, SupervisorRequestsError>> CancelAsync(
        Guid id,
        Guid studentUserId,
        CancellationToken ct)
    {
        var studentId = await repository.GetStudentIdByUserIdAsync(studentUserId, ct);
        if (studentId is null)
            return Result<bool, SupervisorRequestsError>.Fail(
                SupervisorRequestsError.NotFound,
                "Student profile not found");

        var entity = await repository.GetByIdWithTrackingAsync(id, ct);
        if (entity is null)
            return Result<bool, SupervisorRequestsError>.Fail(
                SupervisorRequestsError.NotFound,
                "Supervisor request not found");

        if (entity.StudentId != studentId.Value)
            return Result<bool, SupervisorRequestsError>.Fail(
                SupervisorRequestsError.Forbidden,
                "You can only cancel your own supervisor request");

        var currentStatus = entity.Status.CodeName;
        if (!string.Equals(currentStatus, PendingStatus, StringComparison.Ordinal))
            return Result<bool, SupervisorRequestsError>.Fail(
                SupervisorRequestsError.InvalidTransition,
                $"Cannot cancel request from status '{currentStatus}'");

        var cancelledStatusId = await applicationStatusesRepository.GetIdByCodeNameAsync(CancelledStatus, ct);
        if (cancelledStatusId is null)
            return Result<bool, SupervisorRequestsError>.Fail(
                SupervisorRequestsError.Validation,
                "Status 'Cancelled' not found");

        entity.StatusId = cancelledStatusId.Value;
        await repository.SaveChangesAsync(ct);

        return Result<bool, SupervisorRequestsError>.Ok(true);
    }

    /// <summary>
    /// Преобразует детальное DTO в DTO списка/ответа операции.
    /// </summary>
    private static SupervisorRequestDto ToDto(SupervisorRequestDetailDto detail) =>
        new(
            detail.Id,
            detail.StudentId,
            detail.StudentFirstName,
            detail.StudentLastName,
            detail.TeacherUserId,
            detail.TeacherFirstName,
            detail.TeacherLastName,
            detail.Status,
            detail.Comment,
            detail.CreatedAt,
            detail.UpdatedAt);

    /// <summary>
    /// Упрощённый конструктор ошибки Result.
    /// </summary>
    private static Result<SupervisorRequestDto, SupervisorRequestsError> Fail(
        SupervisorRequestsError error,
        string message)
        => Result<SupervisorRequestDto, SupervisorRequestsError>.Fail(error, message);
}
