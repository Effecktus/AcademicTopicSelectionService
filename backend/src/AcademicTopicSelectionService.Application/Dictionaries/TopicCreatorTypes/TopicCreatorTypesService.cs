using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.TopicCreatorTypes;

public sealed class TopicCreatorTypesService(ITopicCreatorTypesRepository repo) : ITopicCreatorTypesService
{
    /// <inheritdoc />
    public Task<PagedResult<TopicCreatorTypeDto>> ListAsync(ListTopicCreatorTypesQuery query, CancellationToken ct)
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
    public Task<TopicCreatorTypeDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<TopicCreatorTypeDto, TopicCreatorTypesError>> CreateAsync(
        UpsertTopicCreatorTypeCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Fail(TopicCreatorTypesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, null, ct))
        {
            return Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Fail(TopicCreatorTypesError.Conflict,
                "TopicCreatorType with the same CodeName already exists.");
        }

        var created = await repo.CreateAsync(codeName, displayName, ct);
        return Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<TopicCreatorTypeDto, TopicCreatorTypesError>> UpdateAsync(
        Guid id, UpsertTopicCreatorTypeCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Fail(TopicCreatorTypesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Fail(TopicCreatorTypesError.Conflict,
                "TopicCreatorType with the same CodeName already exists.");
        }

        var updated = await repo.UpdateAsync(id, codeName, displayName, ct);
        return updated is null
            ? Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Fail(TopicCreatorTypesError.NotFound, "TopicCreatorType not found")
            : Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<TopicCreatorTypeDto, TopicCreatorTypesError>> PatchAsync(
        Guid id, UpsertTopicCreatorTypeCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.ValidatePatch(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Fail(TopicCreatorTypesError.Validation, error);
        }

        if (codeName is not null && await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Fail(TopicCreatorTypesError.Conflict,
                "TopicCreatorType with the same CodeName already exists.");
        }

        var patched = await repo.PatchAsync(id, codeName, displayName, ct);
        return patched is null
            ? Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Fail(TopicCreatorTypesError.NotFound, "TopicCreatorType not found")
            : Result<TopicCreatorTypeDto, TopicCreatorTypesError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);
}
