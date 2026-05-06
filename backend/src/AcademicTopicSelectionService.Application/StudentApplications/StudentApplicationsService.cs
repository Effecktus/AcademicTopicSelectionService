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
    public Task<StudentApplicationDetailDto?> GetDetailForViewerAsync(
        Guid id, string roleCodeName, Guid userId, CancellationToken ct)
        => appRepo.GetDetailForViewerAsync(id, roleCodeName, userId, ct);

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
            return Fail(ApplicationsError.Validation,
                "Student profile not found. Ask an administrator to create your student profile.");

        // 3. Проверить что запрос на научрука существует, принадлежит студенту и одобрен
        var approvedSupervisorRequest = await appRepo.GetApprovedSupervisorRequestAsync(
            command.SupervisorRequestId, studentProfileId.Value, ct);
        if (approvedSupervisorRequest is null)
            return Fail(ApplicationsError.Validation, "Approved supervisor request not found for this student");

        if (await appRepo.StudentHasActiveApplicationAsync(studentProfileId.Value, ct))
            return Fail(ApplicationsError.Validation, "Student already has an active application");

        // 4. Получить/создать тему
        Guid topicId;
        string topicTitle;
        string? topicDescription;
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

            var topic = await topicRepo.GetAsync(topicId, ct);
            if (topic is null)
                return Fail(ApplicationsError.NotFound, "Topic not found");

            if (!await topicRepo.IsCreatedByUserAsync(topicId, approvedSupervisorRequest.TeacherUserId, ct))
                return Fail(ApplicationsError.Validation,
                    "Selected topic does not belong to the approved supervisor");

            topicTitle = topic.Title;
            topicDescription = string.IsNullOrWhiteSpace(topic.Description)
                ? null
                : topic.Description.Trim();
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
                Id = Guid.NewGuid(),
                Title = proposedTitle,
                Description = proposedDescription,
                CreatorTypeId = studentCreatorTypeId.Value,
                CreatedBy = studentUserId,
                StatusId = activeTopicStatusId.Value
            };

            var createdTopic = await topicRepo.AddAsync(newTopic, ct);
            topicId = createdTopic.Id;
            topicTitle = createdTopic.Title;
            topicDescription = string.IsNullOrWhiteSpace(createdTopic.Description)
                ? null
                : createdTopic.Description.Trim();
        }

        // 5. Статус «На редактировании» — действие и уведомление научруку после передачи студентом
        var onEditingStatusId = await appStatusesRepo.GetIdByCodeNameAsync(ApplicationStatusCodes.OnEditing, ct);
        if (onEditingStatusId is null)
            return Fail(ApplicationsError.Validation, "Application status 'OnEditing' not found");

        // 6. Создать заявку
        var application = new StudentApplication
        {
            Id = Guid.NewGuid(),
            StudentId = studentProfileId.Value,
            TopicId = topicId,
            SupervisorRequestId = approvedSupervisorRequest.Id,
            StatusId = onEditingStatusId.Value,
        };

        await appRepo.AddAsync(application, ct);
        appRepo.StageApplicationTopicChangeHistory(new ApplicationTopicChangeHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            ChangedByUserId = studentUserId,
            ChangeKind = ApplicationTopicChangeKinds.TopicTitle,
            NewValue = topicTitle.Trim(),
        });
        appRepo.StageApplicationTopicChangeHistory(new ApplicationTopicChangeHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            ChangedByUserId = studentUserId,
            ChangeKind = ApplicationTopicChangeKinds.TopicDescription,
            NewValue = topicDescription,
        });
        await appRepo.SaveChangesAsync(ct);

        // 7. Вернуть DTO
        var dto = await appRepo.GetDetailAsync(application.Id, ct);
        if (dto is null)
            return Fail(ApplicationsError.NotFound, "Application was created but not found");

        return Result<StudentApplicationDto, ApplicationsError>.Ok(StudentApplicationDto.FromDetail(dto));
    }

    /// <inheritdoc />
    public async Task<Result<StudentApplicationDto, ApplicationsError>> SubmitToSupervisorAsync(
        Guid applicationId, Guid studentUserId, CancellationToken ct)
    {
        var user = await usersRepo.GetByIdAsync(studentUserId, ct);
        if (user is null)
            return Fail(ApplicationsError.NotFound, "User not found");
        if (user.Role.CodeName != UserRoleCodes.Student)
            return Fail(ApplicationsError.Forbidden, "Only students can submit applications");

        var studentProfileId = await GetStudentIdByUserIdAsync(studentUserId, ct);
        if (studentProfileId is null)
            return Fail(ApplicationsError.Validation, "Student profile not found");

        var appDetail = await appRepo.GetDetailAsync(applicationId, ct);
        if (appDetail is null)
            return Fail(ApplicationsError.NotFound, "Application not found");
        if (appDetail.StudentId != studentProfileId.Value)
            return Fail(ApplicationsError.Forbidden, "You can only submit your own application");
        if (appDetail.Status.CodeName != ApplicationStatusCodes.OnEditing)
            return Fail(ApplicationsError.InvalidTransition,
                $"Cannot submit from '{appDetail.Status.DisplayName}' — expected '{ApplicationStatusCodes.OnEditing}'");

        var pendingStatusId = await appStatusesRepo.GetIdByCodeNameAsync(ApplicationStatusCodes.Pending, ct);
        if (pendingStatusId is null)
            return Fail(ApplicationsError.Validation, "Application status 'Pending' not found");

        var pendingActionStatusId = await actionRepo.GetActionStatusIdByCodeNameAsync(ApplicationActionStatusCodes.Pending, ct);
        if (pendingActionStatusId is null)
            return Fail(ApplicationsError.Validation, $"Action status '{ApplicationActionStatusCodes.Pending}' not found");

        var app = await appRepo.GetByIdWithTrackingAsync(applicationId, ct);
        if (app is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        app.StatusId = pendingStatusId.Value;
        actionRepo.Enqueue(applicationId, appDetail.SupervisorUserId, pendingActionStatusId.Value, null);

        var supervisorNotification = await notificationsService.CreateAsync(
            new CreateNotificationCommand(
                appDetail.SupervisorUserId,
                NotificationTypeCodes.ApplicationSubmittedToSupervisor,
                "Новая заявка на тему ВКР",
                $"Студент {user.FirstName} {user.LastName} передал на рассмотрение заявку на тему «{appDetail.TopicTitle}»."),
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

        var updatedDto = await appRepo.GetDetailAsync(applicationId, ct);
        if (updatedDto is null)
            return Fail(ApplicationsError.NotFound, "Application not found after update");

        return Result<StudentApplicationDto, ApplicationsError>.Ok(StudentApplicationDto.FromDetail(updatedDto));
    }

    /// <inheritdoc />
    public async Task<Result<StudentApplicationDto, ApplicationsError>> UpdateTopicAsync(
        Guid applicationId, UpdateApplicationTopicCommand command, Guid studentUserId, CancellationToken ct)
    {
        var user = await usersRepo.GetByIdAsync(studentUserId, ct);
        if (user is null)
            return Fail(ApplicationsError.NotFound, "User not found");
        if (user.Role.CodeName != UserRoleCodes.Student)
            return Fail(ApplicationsError.Forbidden, "Only students can update the topic");

        var studentProfileId = await GetStudentIdByUserIdAsync(studentUserId, ct);
        if (studentProfileId is null)
            return Fail(ApplicationsError.Validation, "Student profile not found");

        var appDetail = await appRepo.GetDetailAsync(applicationId, ct);
        if (appDetail is null)
            return Fail(ApplicationsError.NotFound, "Application not found");
        if (appDetail.StudentId != studentProfileId.Value)
            return Fail(ApplicationsError.Forbidden, "You can only edit your own application");
        if (appDetail.Status.CodeName != ApplicationStatusCodes.OnEditing)
            return Fail(ApplicationsError.InvalidTransition,
                $"Topic can only be edited when application is '{ApplicationStatusCodes.OnEditing}'");

        var title = command.Title.Trim();
        if (title.Length == 0)
            return Fail(ApplicationsError.Validation, "Title is required");
        if (title.Length > 500)
            return Fail(ApplicationsError.Validation, "Title must be <= 500 characters");

        var description = string.IsNullOrWhiteSpace(command.Description)
            ? null
            : command.Description.Trim();

        var topic = await topicRepo.GetByIdForUpdateAsync(appDetail.TopicId, ct);
        if (topic is null)
            return Fail(ApplicationsError.NotFound, "Topic not found");

        var previousTitle = topic.Title.Trim();
        var previousDescription = string.IsNullOrWhiteSpace(topic.Description)
            ? null
            : topic.Description.Trim();

        topic.Title = title;
        topic.Description = description;

        if (!string.Equals(previousTitle, title, StringComparison.Ordinal))
        {
            appRepo.StageApplicationTopicChangeHistory(new ApplicationTopicChangeHistory
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                ChangedByUserId = studentUserId,
                ChangeKind = ApplicationTopicChangeKinds.TopicTitle,
                NewValue = title,
            });
        }

        if (previousDescription != description)
        {
            appRepo.StageApplicationTopicChangeHistory(new ApplicationTopicChangeHistory
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                ChangedByUserId = studentUserId,
                ChangeKind = ApplicationTopicChangeKinds.TopicDescription,
                NewValue = description,
            });
        }

        await topicRepo.SaveChangesAsync(ct);

        var updatedDto = await appRepo.GetDetailAsync(applicationId, ct);
        if (updatedDto is null)
            return Fail(ApplicationsError.NotFound, "Application not found after update");

        return Result<StudentApplicationDto, ApplicationsError>.Ok(StudentApplicationDto.FromDetail(updatedDto));
    }

    /// <inheritdoc />
    public async Task<Result<StudentApplicationDto, ApplicationsError>> ApproveBySupervisorAsync(
        Guid applicationId, ApproveBySupervisorCommand command, Guid callerUserId, CancellationToken ct)
    {
        var check = await VerifySupervisorAsync(applicationId, callerUserId, ct);
        if (check is not null) return check;

        var appDetail = await appRepo.GetDetailAsync(applicationId, ct);
        if (appDetail is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        if (appDetail.Status.CodeName != ApplicationStatusCodes.Pending)
            return Fail(ApplicationsError.InvalidTransition,
                $"Cannot transition from '{appDetail.Status.DisplayName}' — expected '{ApplicationStatusCodes.Pending}'");

        var supervisor = await usersRepo.GetByIdAsync(callerUserId, ct);
        if (supervisor?.DepartmentId is null)
            return Fail(ApplicationsError.Validation,
                "Supervisor has no department. Cannot submit to department head.");

        var deptHeadUserId = await usersRepo.GetDepartmentHeadIdAsync(supervisor.DepartmentId.Value, ct);
        if (!deptHeadUserId.HasValue)
            return Fail(ApplicationsError.Validation,
                "Department head not found for supervisor department.");

        var toStatusId = await appStatusesRepo.GetIdByCodeNameAsync(ApplicationStatusCodes.PendingDepartmentHead, ct);
        if (toStatusId is null)
            return Fail(ApplicationsError.Validation, $"Status '{ApplicationStatusCodes.PendingDepartmentHead}' not found");

        var approvedActionStatusId = await actionRepo.GetActionStatusIdByCodeNameAsync(ApplicationActionStatusCodes.Approved, ct);
        if (approvedActionStatusId is null)
            return Fail(ApplicationsError.Validation, $"Action status '{ApplicationActionStatusCodes.Approved}' not found");

        var pendingActionStatusId = await actionRepo.GetActionStatusIdByCodeNameAsync(ApplicationActionStatusCodes.Pending, ct);
        if (pendingActionStatusId is null)
            return Fail(ApplicationsError.Validation, $"Action status '{ApplicationActionStatusCodes.Pending}' not found");

        var app = await appRepo.GetByIdWithTrackingAsync(applicationId, ct);
        if (app is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        var currentAction = await actionRepo.GetLatestPendingByApplicationAndResponsibleAsync(applicationId, callerUserId, ct);
        if (currentAction is null)
            return Fail(ApplicationsError.InvalidTransition, "No pending approval action found for current supervisor");

        var comment = NormalizeOptionalComment(command.Comment);
        actionRepo.UpdateTracked(currentAction, approvedActionStatusId.Value, comment);
        app.StatusId = toStatusId.Value;

        actionRepo.Enqueue(applicationId, deptHeadUserId.Value, pendingActionStatusId.Value, null);

        var deptHeadNotification = await notificationsService.CreateAsync(
            new CreateNotificationCommand(
                deptHeadUserId.Value,
                NotificationTypeCodes.ApplicationSubmittedToDepartmentHead,
                "Новая заявка на рассмотрение",
                AppendCommentLine(
                    $"Научный руководитель передал заявку студента {appDetail.StudentFirstName} {appDetail.StudentLastName} " +
                    $"на рассмотрение. Тема: «{appDetail.TopicTitle}».",
                    comment)),
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

        var updatedDto = await appRepo.GetDetailAsync(applicationId, ct);
        if (updatedDto is null)
            return Fail(ApplicationsError.NotFound, "Application not found after update");

        return Result<StudentApplicationDto, ApplicationsError>.Ok(StudentApplicationDto.FromDetail(updatedDto));
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
    public async Task<Result<StudentApplicationDto, ApplicationsError>> ReturnForEditingBySupervisorAsync(
        Guid applicationId, ReturnApplicationForEditingCommand command, Guid callerUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Comment))
            return Fail(ApplicationsError.Validation, "Comment is required");

        var check = await VerifySupervisorAsync(applicationId, callerUserId, ct);
        if (check is not null) return check;

        return await ReturnForEditingCoreAsync(
            applicationId,
            callerUserId,
            fromStatus: ApplicationStatusCodes.Pending,
            comment: command.Comment.Trim(),
            studentMessageIntro: "Научный руководитель вернул заявку на редактирование. Внесите правки и снова передайте заявку на рассмотрение.",
            ct: ct);
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
    public async Task<Result<StudentApplicationDto, ApplicationsError>> ReturnForEditingByDepartmentHeadAsync(
        Guid applicationId, ReturnApplicationForEditingCommand command, Guid callerUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Comment))
            return Fail(ApplicationsError.Validation, "Comment is required");

        var deptCheck = await VerifyDepartmentHeadAsync(applicationId, callerUserId, ct);
        if (deptCheck is not null) return deptCheck;

        return await ReturnForEditingCoreAsync(
            applicationId,
            callerUserId,
            fromStatus: ApplicationStatusCodes.PendingDepartmentHead,
            comment: command.Comment.Trim(),
            studentMessageIntro: "Заведующий кафедрой вернул заявку на редактирование. Внесите правки и снова передайте заявку научному руководителю.",
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

        // Можно отменить из Pending, ApprovedBySupervisor или OnEditing
        var currentStatus = appDetail.Status.CodeName;
        if (currentStatus != ApplicationStatusCodes.Pending &&
            currentStatus != ApplicationStatusCodes.ApprovedBySupervisor &&
            currentStatus != ApplicationStatusCodes.OnEditing)
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

    private async Task<Result<StudentApplicationDto, ApplicationsError>> ReturnForEditingCoreAsync(
        Guid applicationId,
        Guid callerUserId,
        string fromStatus,
        string comment,
        string studentMessageIntro,
        CancellationToken ct)
    {
        var appDetail = await appRepo.GetDetailAsync(applicationId, ct);
        if (appDetail is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        if (appDetail.Status.CodeName != fromStatus)
            return Fail(ApplicationsError.InvalidTransition,
                $"Cannot transition from '{appDetail.Status.DisplayName}' — expected '{fromStatus}'");

        var toStatusId = await appStatusesRepo.GetIdByCodeNameAsync(ApplicationStatusCodes.OnEditing, ct);
        if (toStatusId is null)
            return Fail(ApplicationsError.Validation, $"Status '{ApplicationStatusCodes.OnEditing}' not found");

        var actionStatusId = await actionRepo.GetActionStatusIdByCodeNameAsync(
            ApplicationActionStatusCodes.ReturnedForEditing, ct);
        if (actionStatusId is null)
            return Fail(ApplicationsError.Validation, $"Action status '{ApplicationActionStatusCodes.ReturnedForEditing}' not found");

        var normalizedComment = NormalizeOptionalComment(comment);

        var app = await appRepo.GetByIdWithTrackingAsync(applicationId, ct);
        if (app is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        var currentAction = await actionRepo.GetLatestPendingByApplicationAndResponsibleAsync(
            applicationId, callerUserId, ct);
        if (currentAction is null)
            return Fail(ApplicationsError.InvalidTransition, "No pending action found for current approver");

        app.StatusId = toStatusId.Value;
        actionRepo.UpdateTracked(currentAction, actionStatusId.Value, normalizedComment);

        Notification? queuedNotification = null;
        if (app.Student is not null)
        {
            queuedNotification = await notificationsService.CreateAsync(
                new CreateNotificationCommand(
                    app.Student.UserId,
                    NotificationTypeCodes.ApplicationStatusChanged,
                    "Заявка возвращена на редактирование",
                    AppendCommentLine(studentMessageIntro, normalizedComment)),
                ct);
        }

        await appRepo.SaveChangesAsync(ct);

        if (queuedNotification is not null)
        {
            await notificationsService.EnqueueEmailAsync(
                queuedNotification.UserId,
                queuedNotification.Title,
                queuedNotification.Content,
                ct);
        }

        var updatedDto = await appRepo.GetDetailAsync(applicationId, ct);
        if (updatedDto is null)
            return Fail(ApplicationsError.NotFound, "Application not found after update");

        return Result<StudentApplicationDto, ApplicationsError>.Ok(StudentApplicationDto.FromDetail(updatedDto));
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

        var normalizedComment = NormalizeOptionalComment(comment);

        // Обновить заявку и текущий action — без сохранения
        var app = await appRepo.GetByIdWithTrackingAsync(applicationId, ct);
        if (app is null)
            return Fail(ApplicationsError.NotFound, "Application not found");

        var currentAction = await actionRepo.GetLatestPendingByApplicationAndResponsibleAsync(applicationId, callerUserId, ct);
        if (currentAction is null)
            return Fail(ApplicationsError.InvalidTransition, "No pending action found for current approver");

        app.StatusId = toStatusId.Value;
        actionRepo.UpdateTracked(currentAction, actionStatusId.Value, normalizedComment);

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
                    AppendCommentLine(content, normalizedComment)),
                ct);
        }

        Notification? queuedSupervisorNotification = null;
        var shouldNotifySupervisor = toStatus is ApplicationStatusCodes.ApprovedByDepartmentHead
            or ApplicationStatusCodes.RejectedByDepartmentHead;
        if (shouldNotifySupervisor && app.SupervisorRequest is not null)
        {
            var (title, content) = BuildSupervisorNotificationMessage(toStatus);
            queuedSupervisorNotification = await notificationsService.CreateAsync(
                new CreateNotificationCommand(
                    app.SupervisorRequest.TeacherUserId,
                    NotificationTypeCodes.SupervisorDecisionByDepartmentHead,
                    title,
                    AppendCommentLine(content, normalizedComment)),
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

        if (queuedSupervisorNotification is not null)
        {
            await notificationsService.EnqueueEmailAsync(
                queuedSupervisorNotification.UserId,
                queuedSupervisorNotification.Title,
                queuedSupervisorNotification.Content,
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

    private static (string Title, string Content) BuildSupervisorNotificationMessage(string toStatus)
    {
        return toStatus switch
        {
            ApplicationStatusCodes.ApprovedByDepartmentHead => (
                "Заявка вашего студента утверждена заведующим кафедрой",
                "Заведующий кафедрой утвердил заявку вашего студента."),
            ApplicationStatusCodes.RejectedByDepartmentHead => (
                "Заявка вашего студента отклонена заведующим кафедрой",
                "Заведующий кафедрой отклонил заявку вашего студента."),
            _ => (
                "Статус заявки студента изменен",
                $"Статус заявки вашего студента изменен: {toStatus}.")
        };
    }

    private static string? NormalizeOptionalComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return null;

        return comment.Trim();
    }

    private static string AppendCommentLine(string content, string? comment)
    {
        var normalizedComment = NormalizeOptionalComment(comment);
        if (normalizedComment is null)
            return content;

        return $"{content}\nКомментарий: {normalizedComment}";
    }

    private static Result<StudentApplicationDto, ApplicationsError> Fail(ApplicationsError error, string message) =>
        Result<StudentApplicationDto, ApplicationsError>.Fail(error, message);
}
