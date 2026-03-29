using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.StudyGroups;

/// <inheritdoc />
/// <param name="repo">Репозиторий для работы с данными групп.</param>
public sealed class StudyGroupsService(IStudyGroupsRepository repo) : IStudyGroupsService
{
    private const int MinCodeName = 1000;
    private const int MaxCodeName = 9999;

    /// <inheritdoc />
    public Task<PagedResult<StudyGroupDto>> ListAsync(ListStudyGroupsQuery query, CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200)
        };

        return repo.ListAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<StudyGroupDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<StudyGroupDto, StudyGroupsError>> CreateAsync(
        UpsertStudyGroupCommand command, CancellationToken ct)
    {
        var (ok, codeName, error) = ValidateRequired(command);
        if (!ok)
        {
            return Result<StudyGroupDto, StudyGroupsError>.Fail(StudyGroupsError.Validation, error);
        }

        if (await repo.ExistsByCodeNameAsync(codeName, null, ct))
        {
            return Result<StudyGroupDto, StudyGroupsError>.Fail(
                StudyGroupsError.Conflict, "StudyGroup with the same CodeName already exists");
        }

        var created = await repo.CreateAsync(codeName, ct);
        return Result<StudyGroupDto, StudyGroupsError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<StudyGroupDto, StudyGroupsError>> UpdateAsync(
        Guid id, UpsertStudyGroupCommand command, CancellationToken ct)
    {
        var (ok, codeName, error) = ValidateRequired(command);
        if (!ok)
        {
            return Result<StudyGroupDto, StudyGroupsError>.Fail(StudyGroupsError.Validation, error);
        }

        if (await repo.ExistsByCodeNameAsync(codeName, id, ct))
        {
            return Result<StudyGroupDto, StudyGroupsError>.Fail(
                StudyGroupsError.Conflict, "StudyGroup with the same CodeName already exists");
        }

        var updated = await repo.UpdateAsync(id, codeName, ct);
        return updated is null
            ? Result<StudyGroupDto, StudyGroupsError>.Fail(StudyGroupsError.NotFound, "StudyGroup not found")
            : Result<StudyGroupDto, StudyGroupsError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<StudyGroupDto, StudyGroupsError>> PatchAsync(
        Guid id, UpsertStudyGroupCommand command, CancellationToken ct)
    {
        if (command.CodeName is null)
        {
            return Result<StudyGroupDto, StudyGroupsError>.Fail(
                StudyGroupsError.Validation, "At least one field must be provided for patch");
        }

        var (ok, codeName, error) = ValidateRequired(command);
        if (!ok)
        {
            return Result<StudyGroupDto, StudyGroupsError>.Fail(StudyGroupsError.Validation, error);
        }

        if (await repo.ExistsByCodeNameAsync(codeName, id, ct))
        {
            return Result<StudyGroupDto, StudyGroupsError>.Fail(
                StudyGroupsError.Conflict, "StudyGroup with the same CodeName already exists");
        }

        var patched = await repo.PatchAsync(id, codeName, ct);
        return patched is null
            ? Result<StudyGroupDto, StudyGroupsError>.Fail(StudyGroupsError.NotFound, "StudyGroup not found")
            : Result<StudyGroupDto, StudyGroupsError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);

    /// <summary>
    /// Валидирует обязательное поле CodeName команды.
    /// </summary>
    private static (bool ok, int codeName, string error) ValidateRequired(UpsertStudyGroupCommand command)
    {
        if (command.CodeName is null)
        {
            return (false, 0, "CodeName is required");
        }

        var value = command.CodeName.Value;
        if (value is < MinCodeName or > MaxCodeName)
        {
            return (false, 0, $"CodeName must be between {MinCodeName} and {MaxCodeName}");
        }

        return (true, value, string.Empty);
    }
}
