using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

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
        var (ok, codeName, displayName, shortName, error) = DictionaryCodeDisplayValidator.ValidateWithOptionalShortName(
            command.CodeName, command.DisplayName, command.ShortName);
        if (!ok)
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, null, ct))
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Conflict,
                "AcademicDegree with the same CodeName already exists.");
        }

        var created = await repo.CreateAsync(codeName, displayName, shortName, ct);
        return Result<AcademicDegreeDto, AcademicDegreesError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<AcademicDegreeDto, AcademicDegreesError>> UpdateAsync(
        Guid id, UpsertAcademicDegreeCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, shortName, error) = DictionaryCodeDisplayValidator.ValidateWithOptionalShortName(
            command.CodeName, command.DisplayName, command.ShortName);
        if (!ok)
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Conflict,
                "AcademicDegree with the same CodeName already exists.");
        }

        var updated = await repo.UpdateAsync(id, codeName, displayName, shortName, ct);
        return updated is null
            ? Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.NotFound, "AcademicDegree not found")
            : Result<AcademicDegreeDto, AcademicDegreesError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<AcademicDegreeDto, AcademicDegreesError>> PatchAsync(
        Guid id, UpsertAcademicDegreeCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, shortName, error) = DictionaryCodeDisplayValidator.ValidatePatchWithOptionalShortName(
            command.CodeName, command.DisplayName, command.ShortName);
        if (!ok)
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Validation, error);
        }

        if (codeName is not null && await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.Conflict,
                "AcademicDegree with the same CodeName already exists.");
        }

        var patched = await repo.PatchAsync(id, codeName, displayName, shortName, ct);
        return patched is null
            ? Result<AcademicDegreeDto, AcademicDegreesError>.Fail(AcademicDegreesError.NotFound, "AcademicDegree not found")
            : Result<AcademicDegreeDto, AcademicDegreesError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);
}
