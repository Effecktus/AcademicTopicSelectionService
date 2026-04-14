using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Application.StudentApplications;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.StudentApplications;

/// <summary>
/// Реализация сервиса заявок студентов.
/// </summary>
/// <param name="appRepo">Репозиторий заявок.</param>
/// <param name="topicRepo">Репозиторий тем (для проверки существования и блокировки).</param>
/// <param name="actionRepo">Репозиторий действий по заявкам.</param>
/// <param name="usersRepo">Репозиторий пользователей (для проверки ролей и кафедр).</param>
/// <param name="appStatusesRepo">Репозиторий статусов заявок (резолв ID по CodeName).</param>
public sealed class StudentApplicationsService(
    IStudentApplicationsRepository appRepo,
    ITopicsRepository topicRepo,
    ITopicCreatorTypesRepository topicCreatorTypesRepo,
    ITopicStatusesRepository topicStatusesRepo,
    IApplicationActionsRepository actionRepo,
    IUsersRepository usersRepo,
    IApplicationStatusesRepository appStatusesRepo,
    INotificationsService notificationsService) : IStudentApplicationsService
{
    // Терминальные статусы — из них нельзя перейти в другие
    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ApplicationStatusCodes.RejectedBySupervisor,
        ApplicationStatusCodes.RejectedByDepartmentHead,
        ApplicationStatusCodes.Cancelled,
        ApplicationStatusCodes.ApprovedByDepartmentHead
    };

    /// <inheritdoc />
    public Task<PagedResult<StudentApplicationDto>> ListForRoleAsync(
        ListApplicationsQuery query, string roleCodeName, Guid userId, CancellationToken ct)
        => appRepo.ListForRoleAsync(query, roleCodeName, userId, ct);

    /// <inheritdoc />
    public Task<StudentApplicationDetailDto?> GetDetailAsync(Guid id, CancellationToken ct)
        => appRepo.GetDetailAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<StudentApplicationDto, ApplicationsError>> CreateAsync(
        CreateApplicationCommand command, Guid studentUserId, CancellationToken ct)
    {
        // 1. Проверить что пользователь существует и имеет роль Student
        var user = await usersRepo.GetByIdAsync(studentUserId, ct);
        if (user is null)
            return Fail(ApplicationsError.NotFound, "User not found");

        if (user.Role.CodeName != UserRoleCodes.Student)
            return Fail(ApplicationsError.Forbidden, "Only students can create applications");

        if (command.SupervisorRequestId == Guid.Empty)
            return Fail(ApplicationsError.Validation, "SupervisorRequestId is required");

        var hasTopicId = command.TopicId.HasValue && command.TopicId.Value != Guid.Empty;
        var hasProposedTitle = !string.IsNullOrWhiteSpace(command.ProposedTitle);
        if (hasTopicId == hasProposedTitle)
            return Fail(ApplicationsError.Validation, "Provide either TopicId or ProposedTitle");

        // 2. Проверить что у студента нет активной заявки
        var studentProfileId = await GetStudentIdByUserIdAsync(studentUserId, ct);
        if (studentProfileId is null)
            return Fail(ApplicationsError.Validation, "Student profile not found. Register as student first.");

        // 3. Проверить что запрос на научрука существует, принадлежит студенту и одобрен
        var approvedSupervisorRequest = await appRepo.GetApprovedSupervisorRequestAsync(
            command.SupervisorRequestId, studentProfileId.Value, ct);
        if (approvedSupervisorRequest is null)
            return Fail(ApplicationsError.Validation, "Approved supervisor request not found for this student");

        if (await appRepo.StudentHasActiveApplicationAsync(studentProfileId.Value, ct))
            return Fail(ApplicationsError.Validation, "Student already has an active application");

        // 4. Получить/создать тему
        Guid topicId;
        if (hasTopicId)
        {
            topicId = command.TopicId!.Value;
            var topicExists = await topicRepo.ExistsByIdAsync(topicId, ct);
            if (!topicExists)
                return Fail(ApplicationsError.NotFound, "Topic not found");

            if (!await topicRepo.IsActiveByIdAsync(topicId, ct))
                return Fail(ApplicationsError.Validation, "Topic is not active and does not accept applications");

            if (await appRepo.HasActiveApplicationOnTopicAsync(topicId, ct))
                return Fail(ApplicationsError.Conflict, "Topic is already taken by another student");
        }
        else
        {
            var proposedTitle = command.ProposedTitle!.Trim();
            if (proposedTitle.Length == 0)
                return Fail(ApplicationsError.Validation, "ProposedTitle is required");
            if (proposedTitle.Length > 500)
                return Fail(ApplicationsError.Validation, "ProposedTitle must be <= 500 characters");

            var proposedDescription = string.IsNullOrWhiteSpace(command.ProposedDescription)
                ? null
                : command.ProposedDescription.Trim();

            var studentCreatorTypeId = await topicCreatorTypesRepo.GetIdByCodeNameAsync(TopicCreatorTypeCodes.Student, ct);
            if (studentCreatorTypeId is null)
                return Fail(ApplicationsError.Validation, "Topic creator type 'Student' not found");

            var activeTopicStatusId = await topicStatusesRepo.GetIdByCodeNameAsync(TopicStatusCodes.Active, ct);
            if (activeTopicStatusId is null)
                return Fail(ApplicationsError.Validation, "Topic status 'Active' not found");

            var newTopic = new Topic
            {
                Title = proposedTitle,
                Description = proposedDescription,
                CreatorTypeId = studentCreatorTypeId.Value,
                CreatedBy = studentUserId,
                StatusId = activeTopicStatusId.Value
            };

            var createdTopic = await topicRepo.AddAsync(newTopic, ct);
            topicId = createdTopic.Id;
        }

        // 5. Получить статус Pending
        var pendingStatusId = await appStatusesRepo.GetIdByCodeNameAsync(ApplicationStatusCodes.Pending, ct);
        if (pendingStatusId is null)
            return Fail(ApplicationsError.Validation, "Application status 'Pending' not found");

        // 6. Создать заявку
        var application = new StudentApplication
        {
            Id = Guid.NewGuid(),
            StudentId = studentProfileId.Value,
            TopicId = topicId,
            SupervisorRequestId = approvedSupervisorRequest.Id,
            StatusId = pendingStatusId.Value,
        };

        var created = await appRepo.AddAsync(application, ct);

        // 7. Создать первое действие (Pending)
        var pendingActionStatusId = await actionRepo.GetActionStatusIdByCodeNameAsync(ApplicationActionStatusCodes.Pending, ct);
        if (pendingActionStatusId is not null)
        {
            actionRepo.Enqueue(created.Id, studentUserId, pendingActionStatusId.Value, null);
            await appRepo.SaveChangesAsync(ct);
        }
        else
        {
            await appRepo.SaveChangesAsync(ct);
        }

        // 8. Вернуть DTO
        var dto = await appRepo.GetDetailAsync(created.Id, ct);
        if (dto is null)
            return Fail(ApplicationsError.NotFound, "Application was created but not found");

        var supervisorNotification = await notificationsService.CreateAsync(
            new CreateNotificationCommand(
                approvedSupervisorRequest.TeacherUserId,
                NotificationTypeCodes.ApplicationSubmittedToSupervisor,
                "Новая заявка на тему ВКР",
                $"Студент {user.FirstName} {user.LastName} подал заявку на тему «{dto.TopicTitle}»."),
            ct);

        await appRepo.SaveChangesAsync(ct);

        if (supervisorNotification is not null)
        {
            await notificationsService.EnqueueEmailAsync(
                supervisorNotification.UserId,
                supervisorNotification.Title,
                supervisorNotification.Content,
                ct);
        }

        return Result<StudentApplicationDto, ApplicationsError>.Ok(StudentApplicationDto.FromDetail(dto));
    }

    /// <inheritdoc />
    public async Task<Result<StudentApplicationDto, ApplicationsError>> ApproveBySupervisorAsync(
        Guid applicationId, ApproveBySupervisorCommand command, Guid callerUserId, CancellationToken ct)
    {
        // Проверить что caller = научрук темы
        var check = await VerifySupervisorAsync(applicationId, callerUserId, ct);
        if (check is not null) return check;

        return await TransitionAsync(applicationId, callerUserId,
            fromStatus: ApplicationStatusCodes.Pending,
            toStatus: ApplicationStatusCodes.ApprovedBySupervisor,
            actionStatusCode: ApplicationActionStatusCodes.Approved,
            comment: command.Comment,
            ct: ct);
    }

    /// <inheritdoc />
    public async Task<Result<StudentApplicationDto, ApplicationsError>> RejectBySupervisorAsync(
        Guid applicationId, RejectBySupervisorCommand command, Guid callerUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Comment))
            return Fail(ApplicationsError.Validation, "Rejection reason is required");

        var check = await VerifySupervisorAsync(applicationId, callerUserId, ct);
        if (check is not null) return check;

        return await TransitionAsync(applicationId, callerUserId,
            fromStatus: ApplicationStatusCodes.Pending,
            toStatus: ApplicationStatusCodes.RejectedBySupervisor,
            actionStatusCode: ApplicationActionStatusCodes.Rejected,
            comment: command.Comment.Trim(),
            ct: ct);
    }

    /// <inheritdoc />
    public async Task<Result<StudentApplicationDto, ApplicationsError>> SubmitToDepartmentHeadAsync(
        Guid applicationId, SubmitToDepartmentHeadCommand command, Guid callerUserId, CancellationToken ct)
    {
        var check = await VerifySupervisorAsync(applicationId, callerUserId, ct);
        if (check is not null) return check;

        var appDetail = await appRepo.GetDetailAsync(applicationId, ct);
        if (appDetail is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        // Проверить DepartmentId научрука
        var supervisor = await usersRepo.GetByIdAsync(callerUserId, ct);
        if (supervisor?.DepartmentId is null)
            return Fail(ApplicationsError.Validation,
                "Supervisor has no department. Cannot submit to department head.");

        var result = await TransitionAsync(applicationId, callerUserId,
            fromStatus: ApplicationStatusCodes.ApprovedBySupervisor,
            toStatus: ApplicationStatusCodes.PendingDepartmentHead,
            actionStatusCode: ApplicationActionStatusCodes.Pending,
            comment: command.Comment,
            ct: ct);

        if (result.Error is not null)
            return result;

        var deptHeadUserId = await usersRepo.GetDepartmentHeadIdAsync(supervisor.DepartmentId.Value, ct);
        if (deptHeadUserId.HasValue)
        {
            var deptHeadNotification = await notificationsService.CreateAsync(
                new CreateNotificationCommand(
                    deptHeadUserId.Value,
                    NotificationTypeCodes.ApplicationSubmittedToDepartmentHead,
                    "Новая заявка на рассмотрение",
                    $"Научный руководитель передал заявку студента {appDetail.StudentFirstName} {appDetail.StudentLastName} " +
                    $"на рассмотрение. Тема: «{appDetail.TopicTitle}»."),
                ct);

            await appRepo.SaveChangesAsync(ct);

            if (deptHeadNotification is not null)
            {
                await notificationsService.EnqueueEmailAsync(
                    deptHeadNotification.UserId,
                    deptHeadNotification.Title,
                    deptHeadNotification.Content,
                    ct);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<StudentApplicationDto, ApplicationsError>> ApproveByDepartmentHeadAsync(
        Guid applicationId, ApproveByDepartmentHeadCommand command, Guid callerUserId, CancellationToken ct)
    {
        // Проверить что caller — зав. кафедрой той же кафедры что и научрук
        var deptCheck = await VerifyDepartmentHeadAsync(applicationId, callerUserId, ct);
        if (deptCheck is not null) return deptCheck;

        // Проверить лимит студентов научрука
        var appDetail = await appRepo.GetDetailAsync(applicationId, ct);
        if (appDetail is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        var supervisorUserId = appDetail.SupervisorUserId;
        var occupiedSlots = await appRepo.CountOccupiedSlotsBySupervisorAsync(supervisorUserId, ct);

        // Получить профиль преподавателя
        var teacherProfile = await GetTeacherByUserIdAsync(supervisorUserId, ct);
        if (teacherProfile is not null && teacherProfile.MaxStudentsLimit.HasValue)
        {
            if (occupiedSlots >= teacherProfile.MaxStudentsLimit.Value)
                return Fail(ApplicationsError.SupervisorLimitExceeded,
                    $"Supervisor has reached their limit of {teacherProfile.MaxStudentsLimit.Value} students");
        }

        return await TransitionAsync(applicationId, callerUserId,
            fromStatus: ApplicationStatusCodes.PendingDepartmentHead,
            toStatus: ApplicationStatusCodes.ApprovedByDepartmentHead,
            actionStatusCode: ApplicationActionStatusCodes.Approved,
            comment: command.Comment,
            ct: ct);
    }

    /// <inheritdoc />
    public async Task<Result<StudentApplicationDto, ApplicationsError>> RejectByDepartmentHeadAsync(
        Guid applicationId, RejectByDepartmentHeadCommand command, Guid callerUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Comment))
            return Fail(ApplicationsError.Validation, "Rejection reason is required");

        var deptCheck = await VerifyDepartmentHeadAsync(applicationId, callerUserId, ct);
        if (deptCheck is not null) return deptCheck;

        return await TransitionAsync(applicationId, callerUserId,
            fromStatus: ApplicationStatusCodes.PendingDepartmentHead,
            toStatus: ApplicationStatusCodes.RejectedByDepartmentHead,
            actionStatusCode: ApplicationActionStatusCodes.Rejected,
            comment: command.Comment.Trim(),
            ct: ct);
    }

    /// <inheritdoc />
    public async Task<Result<bool, ApplicationsError>> CancelAsync(
        Guid applicationId, Guid studentUserId, CancellationToken ct)
    {
        // Найти Student.Id по Users.Id
        var studentId = await appRepo.GetStudentIdByUserIdAsync(studentUserId, ct);
        if (studentId is null)
            return Result<bool, ApplicationsError>.Fail(ApplicationsError.NotFound, "Student profile not found");

        // Проверить что студент — владелец заявки
        var appDetail = await appRepo.GetDetailAsync(applicationId, ct);
        if (appDetail is null)
            return Result<bool, ApplicationsError>.Fail(ApplicationsError.NotFound, "Application not found");

        if (appDetail.StudentId != studentId.Value)
            return Result<bool, ApplicationsError>.Fail(ApplicationsError.Forbidden, "You can only cancel your own application");

        // Можно отменить только из Pending или ApprovedBySupervisor
        var currentStatus = appDetail.Status.CodeName;
        if (currentStatus != ApplicationStatusCodes.Pending && currentStatus != ApplicationStatusCodes.ApprovedBySupervisor)
            return Result<bool, ApplicationsError>.Fail(ApplicationsError.InvalidTransition,
                $"Cannot cancel application from status '{appDetail.Status.DisplayName}'");

        var cancelStatusId = await appStatusesRepo.GetIdByCodeNameAsync(ApplicationStatusCodes.Cancelled, ct);
        if (cancelStatusId is null)
            return Result<bool, ApplicationsError>.Fail(ApplicationsError.Validation, "Cancelled status not found");

        var cancelActionStatusId = await actionRepo.GetActionStatusIdByCodeNameAsync(ApplicationActionStatusCodes.Cancelled, ct);
        if (cancelActionStatusId is null)
            return Result<bool, ApplicationsError>.Fail(ApplicationsError.Validation, "Action status 'Cancelled' not found");

        // Обновить статус и добавить action — без промежуточных сохранений
        var app = await appRepo.GetByIdWithTrackingAsync(applicationId, ct);
        if (app is null)
            return Result<bool, ApplicationsError>.Fail(ApplicationsError.NotFound, "Application not found");

        app.StatusId = cancelStatusId.Value;
        actionRepo.Enqueue(applicationId, studentUserId, cancelActionStatusId.Value, null);

        // Единое атомарное сохранение
        await appRepo.SaveChangesAsync(ct);

        return Result<bool, ApplicationsError>.Ok(true);
    }

    // ======================== Helper methods ========================

    /// <summary>
    /// Проверить что callerUserId — научрук темы заявки и что заявка существует.
    /// Возвращает ошибку или null.
    /// </summary>
    private async Task<Result<StudentApplicationDto, ApplicationsError>?> VerifySupervisorAsync(
        Guid applicationId, Guid callerUserId, CancellationToken ct)
    {
        var appDetail = await appRepo.GetDetailAsync(applicationId, ct);
        if (appDetail is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        if (appDetail.SupervisorUserId != callerUserId)
            return Fail(ApplicationsError.Forbidden, "Only the topic supervisor can perform this action");

        return null;
    }

    /// <summary>
    /// Проверить что callerUserId — зав. кафедрой, и кафедра совпадает с кафедрой научрука.
    /// </summary>
    private async Task<Result<StudentApplicationDto, ApplicationsError>?> VerifyDepartmentHeadAsync(
        Guid applicationId, Guid callerUserId, CancellationToken ct)
    {
        var caller = await usersRepo.GetByIdAsync(callerUserId, ct);
        if (caller is null)
            return Fail(ApplicationsError.NotFound, "User not found");

        if (caller.Role.CodeName != UserRoleCodes.DepartmentHead)
            return Fail(ApplicationsError.Forbidden, "Only department head can perform this action");

        var appDetail = await appRepo.GetDetailAsync(applicationId, ct);
        if (appDetail is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        // Получить кафедру научрука
        var supervisor = await usersRepo.GetByIdAsync(appDetail.SupervisorUserId, ct);
        if (supervisor?.DepartmentId is null)
            return Fail(ApplicationsError.Forbidden, "Supervisor has no department");

        if (caller.DepartmentId != supervisor.DepartmentId)
            return Fail(ApplicationsError.Forbidden, "You are not the department head of this supervisor");

        return null;
    }

    /// <summary>
    /// Выполнить переход статуса заявки. Обновление статуса и запись действия — одна атомарная операция.
    /// </summary>
    private async Task<Result<StudentApplicationDto, ApplicationsError>> TransitionAsync(
        Guid applicationId,
        Guid callerUserId,
        string fromStatus,
        string toStatus,
        string actionStatusCode,
        string? comment,
        CancellationToken ct)
    {
        var appDetail = await appRepo.GetDetailAsync(applicationId, ct);
        if (appDetail is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        // Проверить текущий статус
        if (appDetail.Status.CodeName != fromStatus)
            return Fail(ApplicationsError.InvalidTransition,
                $"Cannot transition from '{appDetail.Status.DisplayName}' — expected '{fromStatus}'");

        // Получить ID нового статуса
        var toStatusId = await appStatusesRepo.GetIdByCodeNameAsync(toStatus, ct);
        if (toStatusId is null)
            return Fail(ApplicationsError.Validation, $"Status '{toStatus}' not found");

        // Получить ID статуса действия до сохранения, чтобы записать action атомарно
        var actionStatusId = await actionRepo.GetActionStatusIdByCodeNameAsync(actionStatusCode, ct);
        if (actionStatusId is null)
            return Fail(ApplicationsError.Validation, $"Action status '{actionStatusCode}' not found");

        // Обновить заявку и добавить action — без сохранения
        var app = await appRepo.GetByIdWithTrackingAsync(applicationId, ct);
        if (app is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        app.StatusId = toStatusId.Value;
        actionRepo.Enqueue(applicationId, callerUserId, actionStatusId.Value, comment);

        var shouldNotifyStudent = toStatus is ApplicationStatusCodes.ApprovedBySupervisor
            or ApplicationStatusCodes.RejectedBySupervisor
            or ApplicationStatusCodes.ApprovedByDepartmentHead
            or ApplicationStatusCodes.RejectedByDepartmentHead;
        Notification? queuedNotification = null;
        if (shouldNotifyStudent && app.Student is not null)
        {
            var (title, content) = BuildStudentNotificationMessage(toStatus);
            queuedNotification = await notificationsService.CreateAsync(
                new CreateNotificationCommand(
                    app.Student.UserId,
                    NotificationTypeCodes.ApplicationStatusChanged,
                    title,
                    content),
                ct);
        }

        // Единое атомарное сохранение статуса + action
        await appRepo.SaveChangesAsync(ct);

        if (queuedNotification is not null)
        {
            await notificationsService.EnqueueEmailAsync(
                queuedNotification.UserId,
                queuedNotification.Title,
                queuedNotification.Content,
                ct);
        }

        // Вернуть обновлённый DTO
        var updatedDto = await appRepo.GetDetailAsync(applicationId, ct);
        if (updatedDto is null)
            return Fail(ApplicationsError.NotFound, "Application not found after update");

        return Result<StudentApplicationDto, ApplicationsError>.Ok(StudentApplicationDto.FromDetail(updatedDto));
    }

    private async Task<Guid?> GetStudentIdByUserIdAsync(Guid userId, CancellationToken ct)
    {
        // Пока нет метода — нужен в репозитории студентов
        // Вернём null — репозиторий заявок сделает это сам
        return await appRepo.GetStudentIdByUserIdAsync(userId, ct);
    }

    private async Task<Teacher?> GetTeacherByUserIdAsync(Guid userId, CancellationToken ct)
    {
        // Нужен метод в usersRepo или teachersRepo
        // Для проверки лимита — вернём null если нет
        return await appRepo.GetTeacherByUserIdAsync(userId, ct);
    }

    private static (string Title, string Content) BuildStudentNotificationMessage(string toStatus)
    {
        return toStatus switch
        {
            ApplicationStatusCodes.ApprovedBySupervisor => (
                "Заявка одобрена научным руководителем",
                "Научный руководитель одобрил вашу заявку. Следующий шаг: передача заведующему кафедрой."),
            ApplicationStatusCodes.RejectedBySupervisor => (
                "Заявка отклонена научным руководителем",
                "Научный руководитель отклонил вашу заявку. Проверьте комментарий и при необходимости подайте новую заявку."),
            ApplicationStatusCodes.ApprovedByDepartmentHead => (
                "Заявка утверждена заведующим кафедрой",
                "Заведующий кафедрой утвердил вашу заявку. Тема закреплена за вами."),
            ApplicationStatusCodes.RejectedByDepartmentHead => (
                "Заявка отклонена заведующим кафедрой",
                "Заведующий кафедрой отклонил вашу заявку. Проверьте комментарий и согласуйте дальнейшие действия."),
            _ => (
                "Статус заявки изменен",
                $"Статус вашей заявки изменен: {toStatus}.")
        };
    }

    private static Result<StudentApplicationDto, ApplicationsError> Fail(ApplicationsError error, string message) =>
        Result<StudentApplicationDto, ApplicationsError>.Fail(error, message);
}
