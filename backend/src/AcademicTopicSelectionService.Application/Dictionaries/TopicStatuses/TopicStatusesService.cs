using AcademicTopicSelectionService.Application.Abstractions;

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
        var (ok, name, displayName, error) = Validate(command);
        if (!ok)
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(name, null, ct))
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Conflict,
                "TopicStatus with the same Name already exists.");
        }

        var created = await repo.CreateAsync(name, displayName, ct);
        return Result<TopicStatusDto, TopicStatusesError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<TopicStatusDto, TopicStatusesError>> UpdateAsync(
        Guid id, UpsertTopicStatusCommand command, CancellationToken ct)
    {
        var (ok, name, displayName, error) = Validate(command);
        if (!ok)
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(name, id, ct))
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Conflict,
                "TopicStatus with the same Name already exists.");
        }

        var updated = await repo.UpdateAsync(id, name, displayName, ct);
        return updated is null
            ? Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.NotFound, "TopicStatus not found")
            : Result<TopicStatusDto, TopicStatusesError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<TopicStatusDto, TopicStatusesError>> PatchAsync(
        Guid id, UpsertTopicStatusCommand command, CancellationToken ct)
    {
        var (ok, name, displayName, error) = ValidatePatch(command);
        if (!ok)
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Validation, error);
        }

        if (name is not null && await repo.ExistsByNameAsync(name, id, ct))
        {
            return Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.Conflict,
                "TopicStatus with the same Name already exists.");
        }

        var patched = await repo.PatchAsync(id, name, displayName, ct);
        return patched is null
            ? Result<TopicStatusDto, TopicStatusesError>.Fail(TopicStatusesError.NotFound, "TopicStatus not found")
            : Result<TopicStatusDto, TopicStatusesError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);

    private static (bool ok, string name, string displayName, string error) Validate(UpsertTopicStatusCommand command)
    {
        var name = (command.Name ?? string.Empty).Trim();
        var displayName = (command.DisplayName ?? string.Empty).Trim();

        if (name.Length == 0)
        {
            return (false, string.Empty, string.Empty, "Name is required.");
        }

        return displayName.Length switch
        {
            0 => (false, string.Empty, string.Empty, "DisplayName is required"),
            > 100 => (false, string.Empty, string.Empty, "DisplayName must be <= 100 chars"),
            _ => (true, name, displayName, string.Empty)
        };
    }

    private static (bool ok, string? name, string? displayName, string error) ValidatePatch(
        UpsertTopicStatusCommand command)
    {
        string? name = null;
        string? displayName = null;

        if (command.Name is not null)
        {
            name = command.Name.Trim();
            if (name.Length == 0)
            {
                return (false, null, null, "Name cannot be empty if provided.");
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

        if (name is null && displayName is null)
        {
            return (false, null, null, "At least one field must be provided");
        }

        return (true, name, displayName, string.Empty);
    }
}
