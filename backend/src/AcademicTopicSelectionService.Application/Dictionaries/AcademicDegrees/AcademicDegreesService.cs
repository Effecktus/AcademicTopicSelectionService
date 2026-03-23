using AcademicTopicSelectionService.Application.Abstractions;

namespace AcademicTopicSelectionService.Application.Dictionaries.AcademicDegrees;

public sealed class AcademicDegreesService(IAcademicDegreesRepository repo) : IAcademicDegreesService
{
    /// <inheritdoc />
    public Task<PagedResult<AcademicDegreeDto>> ListAsync(ListAcademicDegreesQuery query, CancellationToken ct)
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
    public Task<AcademicDegreeDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<AcademicDegreeDto, AcademicDegreesError>> CreateAsync(
        UpsertAcademicDegreeCommand command, CancellationToken ct)
    {
        var (ok, name, displayName, shortName, error) = Validate(command);
        if (!ok)
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(name, null, ct))
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Conflict,
                "AcademicDegree with the same Name already exists.");
        }

        var created = await repo.CreateAsync(name, displayName, shortName, ct);
        return Result<AcademicDegreeDto, AcademicDegreesError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<AcademicDegreeDto, AcademicDegreesError>> UpdateAsync(
        Guid id, UpsertAcademicDegreeCommand command, CancellationToken ct)
    {
        var (ok, name, displayName, shortName, error) = Validate(command);
        if (!ok)
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(name, id, ct))
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Conflict,
                "AcademicDegree with the same Name already exists.");
        }

        var updated = await repo.UpdateAsync(id, name, displayName, shortName, ct);
        return updated is null
            ? Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.NotFound, "AcademicDegree not found")
            : Result<AcademicDegreeDto, AcademicDegreesError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<AcademicDegreeDto, AcademicDegreesError>> PatchAsync(
        Guid id, UpsertAcademicDegreeCommand command, CancellationToken ct)
    {
        var (ok, name, displayName, shortName, error) = ValidatePatch(command);
        if (!ok)
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Validation, error);
        }

        if (name is not null && await repo.ExistsByNameAsync(name, id, ct))
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Conflict,
                "AcademicDegree with the same Name already exists.");
        }

        var patched = await repo.PatchAsync(id, name, displayName, shortName, ct);
        return patched is null
            ? Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.NotFound, "AcademicDegree not found")
            : Result<AcademicDegreeDto, AcademicDegreesError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);

    private static (bool ok, string name, string displayName, string? shortName, string error) Validate(UpsertAcademicDegreeCommand command)
    {
        var name = (command.Name ?? string.Empty).Trim();
        var displayName = (command.DisplayName ?? string.Empty).Trim();
        var shortName = string.IsNullOrWhiteSpace(command.ShortName) ? null : command.ShortName.Trim();

        if (name.Length == 0)
        {
            return (false, string.Empty, string.Empty, null, "Name is required.");
        }

        if (displayName.Length == 0)
        {
            return (false, string.Empty, string.Empty, null, "DisplayName is required");
        }

        if (displayName.Length > 100)
        {
            return (false, string.Empty, string.Empty, null, "DisplayName must be <= 100 chars");
        }

        if (shortName is not null && shortName.Length > 50)
        {
            return (false, string.Empty, string.Empty, null, "ShortName must be <= 50 chars");
        }

        return (true, name, displayName, shortName, string.Empty);
    }

    private static (bool ok, string? name, string? displayName, string? shortName, string error) ValidatePatch(
        UpsertAcademicDegreeCommand command)
    {
        string? name = null;
        string? displayName = null;
        string? shortName = null;

        if (command.Name is not null)
        {
            name = command.Name.Trim();
            if (name.Length == 0)
            {
                return (false, null, null, null, "Name cannot be empty if provided.");
            }
        }

        if (command.DisplayName is not null)
        {
            displayName = command.DisplayName.Trim();
            switch (displayName.Length)
            {
                case 0:
                    return (false, null, null, null, "DisplayName cannot be empty if provided");
                case > 100:
                    return (false, null, null, null, "DisplayName must be <= 100 chars");
            }
        }

        if (command.ShortName is not null)
        {
            shortName = string.IsNullOrWhiteSpace(command.ShortName) ? "" : command.ShortName.Trim();
            if (shortName.Length > 50)
            {
                return (false, null, null, null, "ShortName must be <= 50 chars");
            }
        }

        if (name is null && displayName is null && command.ShortName is null)
        {
            return (false, null, null, null, "At least one field must be provided");
        }

        return (true, name, displayName, shortName, string.Empty);
    }
}
