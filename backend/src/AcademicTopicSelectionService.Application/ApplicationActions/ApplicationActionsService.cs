using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.ApplicationActions;

public sealed class ApplicationActionsService(IApplicationActionsRepository repo) : IApplicationActionsService
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<ApplicationActionDto>, ApplicationActionsError>> ListByApplicationAsync(
        ListApplicationActionsQuery query,
        ApplicationActionsActor actor,
        CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200)
        };

        if (!await repo.ApplicationExistsAsync(normalized.ApplicationId, ct))
        {
            return Result<PagedResult<ApplicationActionDto>, ApplicationActionsError>.Fail(
                ApplicationActionsError.ApplicationNotFound, "Application not found.");
        }

        if (!actor.IsAdmin &&
            !await repo.UserCanReadApplicationActionsAsync(normalized.ApplicationId, actor.UserId, ct))
        {
            return Result<PagedResult<ApplicationActionDto>, ApplicationActionsError>.Fail(
                ApplicationActionsError.Forbidden, "You cannot access actions for this application.");
        }

        var page = await repo.ListByApplicationAsync(normalized, ct);
        return Result<PagedResult<ApplicationActionDto>, ApplicationActionsError>.Ok(page);
    }

    /// <inheritdoc />
    public async Task<Result<ApplicationActionDto, ApplicationActionsError>> GetAsync(Guid id,
        ApplicationActionsActor actor,
        CancellationToken ct)
    {
        var dto = await repo.GetAsync(id, ct);
        if (dto is null)
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.NotFound, "ApplicationAction not found.");
        }

        if (!actor.IsAdmin &&
            !await repo.UserCanReadApplicationActionsAsync(dto.ApplicationId, actor.UserId, ct))
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.Forbidden, "You cannot access this application action.");
        }

        return Result<ApplicationActionDto, ApplicationActionsError>.Ok(dto);
    }

    /// <inheritdoc />
    public async Task<Result<ApplicationActionDto, ApplicationActionsError>> CreateAsync(
        CreateApplicationActionCommand command,
        ApplicationActionsActor actor,
        CancellationToken ct)
    {
        if (command.ApplicationId == Guid.Empty)
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.Validation, "ApplicationId is required.");
        }

        if (command.ResponsibleId == Guid.Empty)
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.Validation, "ResponsibleId is required.");
        }

        if (command.Comment is not null && command.Comment.Trim().Length == 0)
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.Validation, "Comment cannot be empty if provided.");
        }

        if (!await repo.ApplicationExistsAsync(command.ApplicationId, ct))
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.ApplicationNotFound, "Application not found.");
        }

        if (!actor.IsAdmin &&
            !await repo.UserCanReadApplicationActionsAsync(command.ApplicationId, actor.UserId, ct))
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.Forbidden, "You cannot create actions for this application.");
        }

        if (!await repo.UserExistsAsync(command.ResponsibleId, ct))
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.ResponsibleUserNotFound, "Responsible user not found.");
        }

        var pendingStatusId = await repo.GetActionStatusIdByCodeNameAsync("Pending", ct);
        if (pendingStatusId is null)
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.StatusNotFound,
                "Default action status 'Pending' not found. Check ApplicationActionStatuses data.");
        }

        var comment = command.Comment?.Trim();
        var created = await repo.CreateAsync(command.ApplicationId, command.ResponsibleId,
            pendingStatusId.Value, comment, ct);

        return Result<ApplicationActionDto, ApplicationActionsError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<ApplicationActionDto, ApplicationActionsError>> UpdateAsync(
        Guid id,
        UpdateApplicationActionCommand command,
        ApplicationActionsActor actor,
        CancellationToken ct)
    {
        if (command.StatusId is null && command.Comment is null)
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.Validation, "At least one field must be provided.");
        }

        if (command.Comment is not null && command.Comment.Trim().Length == 0)
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.Validation, "Comment cannot be empty if provided.");
        }

        var existing = await repo.GetAsync(id, ct);
        if (existing is null)
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.NotFound, "ApplicationAction not found.");
        }

        if (!actor.IsAdmin && existing.ResponsibleId != actor.UserId)
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.Forbidden, "Only the responsible user or an administrator can update this action.");
        }

        if (command.StatusId is not null && !await repo.ActionStatusExistsAsync(command.StatusId.Value, ct))
        {
            return Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.StatusNotFound, "Action status not found.");
        }

        var comment = command.Comment?.Trim();
        var updated = await repo.UpdateAsync(id, command.StatusId, comment, ct);

        return updated is null
            ? Result<ApplicationActionDto, ApplicationActionsError>.Fail(
                ApplicationActionsError.NotFound, "ApplicationAction not found.")
            : Result<ApplicationActionDto, ApplicationActionsError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<bool, ApplicationActionsError>> DeleteAsync(Guid id, ApplicationActionsActor actor,
        CancellationToken ct)
    {
        var existing = await repo.GetAsync(id, ct);
        if (existing is null)
        {
            return Result<bool, ApplicationActionsError>.Fail(
                ApplicationActionsError.NotFound, "ApplicationAction not found.");
        }

        if (!actor.IsAdmin && existing.ResponsibleId != actor.UserId)
        {
            return Result<bool, ApplicationActionsError>.Fail(
                ApplicationActionsError.Forbidden, "Only the responsible user or an administrator can delete this action.");
        }

        var deleted = await repo.DeleteAsync(id, ct);
        return deleted
            ? Result<bool, ApplicationActionsError>.Ok(true)
            : Result<bool, ApplicationActionsError>.Fail(
                ApplicationActionsError.NotFound, "ApplicationAction not found.");
    }
}
