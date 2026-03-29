using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.ApplicationActions;

public sealed class ApplicationActionsService(IApplicationActionsRepository repo) : IApplicationActionsService
{
    /// <inheritdoc />
    public Task<PagedResult<ApplicationActionDto>> ListByApplicationAsync(ListApplicationActionsQuery query,
        CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200)
        };

        return repo.ListByApplicationAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<ApplicationActionDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<ApplicationActionDto, ApplicationActionsError>> CreateAsync(
        CreateApplicationActionCommand command, CancellationToken ct)
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
        Guid id, UpdateApplicationActionCommand command, CancellationToken ct)
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
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);
}
