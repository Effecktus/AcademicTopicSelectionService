using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.NotificationTypes;

public sealed class NotificationTypesService(INotificationTypesRepository repo) : INotificationTypesService
{
    /// <inheritdoc />
    public Task<PagedResult<NotificationTypeDto>> ListAsync(ListNotificationTypesQuery query, CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            Query = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim()
        };

        return repo.ListAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<NotificationTypeDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<NotificationTypeDto, NotificationTypesError>> CreateAsync(
        UpsertNotificationTypeCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<NotificationTypeDto, NotificationTypesError>.Fail(NotificationTypesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, null, ct))
        {
            return Result<NotificationTypeDto, NotificationTypesError>.Fail(NotificationTypesError.Conflict,
                "NotificationType with the same CodeName already exists.");
        }

        var created = await repo.CreateAsync(codeName, displayName, ct);
        return Result<NotificationTypeDto, NotificationTypesError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<NotificationTypeDto, NotificationTypesError>> UpdateAsync(
        Guid id, UpsertNotificationTypeCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<NotificationTypeDto, NotificationTypesError>.Fail(NotificationTypesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<NotificationTypeDto, NotificationTypesError>.Fail(NotificationTypesError.Conflict,
                "NotificationType with the same CodeName already exists.");
        }

        var updated = await repo.UpdateAsync(id, codeName, displayName, ct);
        return updated is null
            ? Result<NotificationTypeDto, NotificationTypesError>.Fail(NotificationTypesError.NotFound, "NotificationType not found")
            : Result<NotificationTypeDto, NotificationTypesError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<NotificationTypeDto, NotificationTypesError>> PatchAsync(
        Guid id, UpsertNotificationTypeCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.ValidatePatch(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<NotificationTypeDto, NotificationTypesError>.Fail(NotificationTypesError.Validation, error);
        }

        if (codeName is not null && await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<NotificationTypeDto, NotificationTypesError>.Fail(NotificationTypesError.Conflict,
                "NotificationType with the same CodeName already exists.");
        }

        var patched = await repo.PatchAsync(id, codeName, displayName, ct);
        return patched is null
            ? Result<NotificationTypeDto, NotificationTypesError>.Fail(NotificationTypesError.NotFound, "NotificationType not found")
            : Result<NotificationTypeDto, NotificationTypesError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);
}
