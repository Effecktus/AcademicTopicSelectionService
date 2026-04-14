using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.Positions;

/// <inheritdoc />
/// <param name="repo">Репозиторий для работы с данными должностей.</param>
public sealed class PositionsService(IPositionsRepository repo) : IPositionsService
{
    /// <inheritdoc />
    public Task<PagedResult<PositionDto>> ListAsync(ListPositionsQuery query, CancellationToken ct)
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
    public Task<PositionDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<PositionDto, PositionsError>> CreateAsync(UpsertPositionCommand command,
        CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<PositionDto, PositionsError>.Fail(PositionsError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, null, ct))
        {
            return Result<PositionDto, PositionsError>.Fail(PositionsError.Conflict,
                "Position with the same CodeName already exists");
        }

        var created = await repo.CreateAsync(codeName, displayName, ct);
        return Result<PositionDto, PositionsError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<PositionDto, PositionsError>> UpdateAsync(Guid id, UpsertPositionCommand command,
        CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<PositionDto, PositionsError>.Fail(PositionsError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<PositionDto, PositionsError>.Fail(PositionsError.Conflict,
                "Position with the same CodeName already exists");
        }

        var updated = await repo.UpdateAsync(id, codeName, displayName, ct);
        return updated is null
            ? Result<PositionDto, PositionsError>.Fail(PositionsError.NotFound, "Position not found")
            : Result<PositionDto, PositionsError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<PositionDto, PositionsError>> PatchAsync(Guid id, UpsertPositionCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.ValidatePatch(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<PositionDto, PositionsError>.Fail(PositionsError.Validation, error);
        }

        if (codeName is not null && await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<PositionDto, PositionsError>.Fail(PositionsError.Conflict, "Position with the same CodeName already exists");
        }

        var patched = await repo.PatchAsync(id, codeName, displayName, ct);
        return patched is null
            ? Result<PositionDto, PositionsError>.Fail(PositionsError.NotFound, "Position not found")
            : Result<PositionDto, PositionsError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);
}
