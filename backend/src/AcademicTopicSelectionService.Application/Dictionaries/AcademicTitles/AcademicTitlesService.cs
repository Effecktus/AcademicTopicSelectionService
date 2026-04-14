using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.AcademicTitles;

public sealed class AcademicTitlesService(IAcademicTitlesRepository repo) : IAcademicTitlesService
{
    /// <inheritdoc />
    public Task<PagedResult<AcademicTitleDto>> ListAsync(ListAcademicTitlesQuery query, CancellationToken ct)
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
    public Task<AcademicTitleDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<AcademicTitleDto, AcademicTitlesError>> CreateAsync(
        UpsertAcademicTitleCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<AcademicTitleDto, AcademicTitlesError>.Fail(AcademicTitlesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, null, ct))
        {
            return Result<AcademicTitleDto, AcademicTitlesError>.Fail(AcademicTitlesError.Conflict,
                "AcademicTitle with the same CodeName already exists.");
        }

        var created = await repo.CreateAsync(codeName, displayName, ct);
        return Result<AcademicTitleDto, AcademicTitlesError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<AcademicTitleDto, AcademicTitlesError>> UpdateAsync(
        Guid id, UpsertAcademicTitleCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<AcademicTitleDto, AcademicTitlesError>.Fail(AcademicTitlesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<AcademicTitleDto, AcademicTitlesError>.Fail(AcademicTitlesError.Conflict,
                "AcademicTitle with the same CodeName already exists.");
        }

        var updated = await repo.UpdateAsync(id, codeName, displayName, ct);
        return updated is null
            ? Result<AcademicTitleDto, AcademicTitlesError>.Fail(AcademicTitlesError.NotFound, "AcademicTitle not found")
            : Result<AcademicTitleDto, AcademicTitlesError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<AcademicTitleDto, AcademicTitlesError>> PatchAsync(
        Guid id, UpsertAcademicTitleCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.ValidatePatch(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<AcademicTitleDto, AcademicTitlesError>.Fail(AcademicTitlesError.Validation, error);
        }

        if (codeName is not null && await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<AcademicTitleDto, AcademicTitlesError>.Fail(AcademicTitlesError.Conflict,
                "AcademicTitle with the same CodeName already exists.");
        }

        var patched = await repo.PatchAsync(id, codeName, displayName, ct);
        return patched is null
            ? Result<AcademicTitleDto, AcademicTitlesError>.Fail(AcademicTitlesError.NotFound, "AcademicTitle not found")
            : Result<AcademicTitleDto, AcademicTitlesError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);
}
