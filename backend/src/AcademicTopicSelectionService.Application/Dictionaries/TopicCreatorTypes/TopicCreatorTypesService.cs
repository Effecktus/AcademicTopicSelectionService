using AcademicTopicSelectionService.Application.Abstractions;

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
        var (ok, codeName, displayName, error) = Validate(command);
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
        var (ok, codeName, displayName, error) = Validate(command);
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
        var (ok, codeName, displayName, error) = ValidatePatch(command);
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

    private static (bool ok, string codeName, string displayName, string error) Validate(UpsertTopicCreatorTypeCommand command)
    {
        var codeName = (command.CodeName ?? string.Empty).Trim();
        var displayName = (command.DisplayName ?? string.Empty).Trim();

        if (codeName.Length == 0)
        {
            return (false, string.Empty, string.Empty, "CodeName is required.");
        }

        return displayName.Length switch
        {
            0 => (false, string.Empty, string.Empty, "DisplayName is required"),
            > 100 => (false, string.Empty, string.Empty, "DisplayName must be <= 100 chars"),
            _ => (true, codeName, displayName, string.Empty)
        };
    }

    private static (bool ok, string? codeName, string? displayName, string error) ValidatePatch(
        UpsertTopicCreatorTypeCommand command)
    {
        string? codeName = null;
        string? displayName = null;

        if (command.CodeName is not null)
        {
            codeName = command.CodeName.Trim();
            if (codeName.Length == 0)
            {
                return (false, null, null, "CodeName cannot be empty if provided.");
            }
        }

        if (command.DisplayName is not null)
        {
            displayName = command.DisplayName.Trim();
            switch (displayName.Length)
            {
                case 0:
                    return (false, null, null, "DisplayName cannot be empty if provided");
                case > 100:
                    return (false, null, null, "DisplayName must be <= 100 chars");
            }
        }

        if (codeName is null && displayName is null)
        {
            return (false, null, null, "At least one field must be provided");
        }

        return (true, codeName, displayName, string.Empty);
    }
}
