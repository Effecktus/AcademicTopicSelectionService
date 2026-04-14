using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.TopicStatuses;

public sealed class TopicStatusesService(ITopicStatusesRepository repo) : ITopicStatusesService
{
    /// <inheritdoc />
    public Task<PagedResult<TopicStatusDto>> ListAsync(ListTopicStatusesQuery query, CancellationToken ct)
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
    public Task<TopicStatusDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<TopicStatusDto, TopicStatusesError>> CreateAsync(
        UpsertTopicStatusCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, null, ct))
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Conflict,
                "TopicStatus with the same CodeName already exists.");
        }

        var created = await repo.CreateAsync(codeName, displayName, ct);
        return Result<TopicStatusDto, TopicStatusesError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<TopicStatusDto, TopicStatusesError>> UpdateAsync(
        Guid id, UpsertTopicStatusCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Conflict,
                "TopicStatus with the same CodeName already exists.");
        }

        var updated = await repo.UpdateAsync(id, codeName, displayName, ct);
        return updated is null
            ? Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.NotFound, "TopicStatus not found")
            : Result<TopicStatusDto, TopicStatusesError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<TopicStatusDto, TopicStatusesError>> PatchAsync(
        Guid id, UpsertTopicStatusCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.ValidatePatch(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Validation, error);
        }

        if (codeName is not null && await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Conflict,
                "TopicStatus with the same CodeName already exists.");
        }

        var patched = await repo.PatchAsync(id, codeName, displayName, ct);
        return patched is null
            ? Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.NotFound, "TopicStatus not found")
            : Result<TopicStatusDto, TopicStatusesError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);
}
