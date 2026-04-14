using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.ApplicationStatuses;

public sealed class ApplicationStatusesService(IApplicationStatusesRepository repo) : IApplicationStatusesService
{
    /// <inheritdoc />
    public Task<PagedResult<ApplicationStatusDto>> ListAsync(ListApplicationStatusQuery query, CancellationToken ct)
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
    public Task<ApplicationStatusDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<ApplicationStatusDto, ApplicationStatusesError>> CreateAsync(
        UpsetApplicationStatusCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<ApplicationStatusDto, ApplicationStatusesError>.Fail(ApplicationStatusesError.Validation, 
                error);
        }

        if (await repo.ExistsByNameAsync(codeName, null, ct))
        {
            return Result<ApplicationStatusDto, ApplicationStatusesError>.Fail(ApplicationStatusesError.Conflict, 
                "ApplicationStatus with the same CodeName already exists.");
        }
        
        var created = await repo.CreateAsync(codeName, displayName, ct);
        return Result<ApplicationStatusDto, ApplicationStatusesError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<ApplicationStatusDto, ApplicationStatusesError>> UpdateAsync(Guid id, 
        UpsetApplicationStatusCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<ApplicationStatusDto, ApplicationStatusesError>.Fail(ApplicationStatusesError.Validation, 
                error);
        }
        
        if (await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<ApplicationStatusDto, ApplicationStatusesError>.Fail(ApplicationStatusesError.Conflict, 
                "ApplicationStatus with the same CodeName already exists.");
        }
        
        var updated = await repo.UpdateAsync(id, codeName, displayName, ct);
        return updated is null
            ? Result<ApplicationStatusDto, ApplicationStatusesError>.Fail(ApplicationStatusesError.NotFound,
                "ApplicationStatus not found")
            : Result<ApplicationStatusDto, ApplicationStatusesError>.Ok(updated);
    }

    public async Task<Result<ApplicationStatusDto, ApplicationStatusesError>> PatchAsync(Guid id, UpsetApplicationStatusCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.ValidatePatch(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<ApplicationStatusDto, ApplicationStatusesError>.Fail(ApplicationStatusesError.Validation, 
                error);
        }
        
        if (codeName is not null && await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<ApplicationStatusDto, ApplicationStatusesError>.Fail(ApplicationStatusesError.Conflict, 
                "ApplicationStatus with the same CodeName already exists.");
        }
        
        var patched = await repo.PatchAsync(id, codeName, displayName, ct);
        return patched is null
            ? Result<ApplicationStatusDto, ApplicationStatusesError>.Fail(ApplicationStatusesError.NotFound,
                "ApplicationStatus not found")
            : Result<ApplicationStatusDto, ApplicationStatusesError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);
}