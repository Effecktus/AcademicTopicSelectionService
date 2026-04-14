using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;

public sealed class ApplicationActionStatusesService(IApplicationActionStatusesRepository repo)
    : IApplicationActionStatusesService
{
    /// <inheritdoc />
    public Task<PagedResult<ApplicationActionStatusDto>> ListAsync(ListApplicationActionStatusQuery query,
        CancellationToken ct)
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
    public Task<ApplicationActionStatusDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<ApplicationActionStatusDto, ApplicationActionStatusesError>> CreateAsync(
        UpsertApplicationActionStatusCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Fail(
                ApplicationActionStatusesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, null, ct))
        {
            return Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Fail(
                ApplicationActionStatusesError.Conflict,
                "ApplicationActionStatus with the same CodeName already exists.");
        }

        var created = await repo.CreateAsync(codeName, displayName, ct);
        return Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<ApplicationActionStatusDto, ApplicationActionStatusesError>> UpdateAsync(
        Guid id, UpsertApplicationActionStatusCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Fail(
                ApplicationActionStatusesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Fail(
                ApplicationActionStatusesError.Conflict,
                "ApplicationActionStatus with the same CodeName already exists.");
        }

        var updated = await repo.UpdateAsync(id, codeName, displayName, ct);
        return updated is null
            ? Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Fail(
                ApplicationActionStatusesError.NotFound, "ApplicationActionStatus not found")
            : Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<ApplicationActionStatusDto, ApplicationActionStatusesError>> PatchAsync(
        Guid id, UpsertApplicationActionStatusCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.ValidatePatch(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Fail(
                ApplicationActionStatusesError.Validation, error);
        }

        if (codeName is not null && await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Fail(
                ApplicationActionStatusesError.Conflict,
                "ApplicationActionStatus with the same CodeName already exists.");
        }

        var patched = await repo.PatchAsync(id, codeName, displayName, ct);
        return patched is null
            ? Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Fail(
                ApplicationActionStatusesError.NotFound, "ApplicationActionStatus not found")
            : Result<ApplicationActionStatusDto, ApplicationActionStatusesError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);
}
