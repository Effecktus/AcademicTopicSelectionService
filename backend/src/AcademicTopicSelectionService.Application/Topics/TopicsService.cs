using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Topics;

/// <inheritdoc />
/// <param name="repo">Репозиторий тем.</param>
/// <param name="topicStatusesRepo">Репозиторий статусов тем.</param>
/// <param name="topicCreatorTypesRepo">Репозиторий типов создателей тем.</param>
public sealed class TopicsService(
    ITopicsRepository repo,
    ITopicStatusesRepository topicStatusesRepo,
    ITopicCreatorTypesRepository topicCreatorTypesRepo) : ITopicsService
{
    /// <inheritdoc />
    public Task<PagedResult<TopicDto>> ListAsync(ListTopicsQuery query, CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            Query = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim(),
            StatusCodeName = string.IsNullOrWhiteSpace(query.StatusCodeName)
                ? null
                : query.StatusCodeName.Trim(),
            CreatorTypeCodeName = string.IsNullOrWhiteSpace(query.CreatorTypeCodeName)
                ? null
                : query.CreatorTypeCodeName.Trim(),
            Sort = string.IsNullOrWhiteSpace(query.Sort) ? null : query.Sort.Trim()
        };

        return repo.ListAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<TopicDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<TopicDto, TopicsError>> CreateAsync(
        CreateTopicCommand command, Guid createdByUserId, CancellationToken ct)
    {
        // Валидация
        var title = command.Title.Trim();
        if (title.Length == 0)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation, "Title is required");
        if (title.Length > 500)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation, "Title must be <= 500 characters");

        var description = string.IsNullOrWhiteSpace(command.Description)
            ? null
            : command.Description.Trim();

        var creatorTypeCodeName = command.CreatorTypeCodeName.Trim();
        if (creatorTypeCodeName.Length == 0)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation, "CreatorTypeCodeName is required");

        var creatorTypeId = await topicCreatorTypesRepo.GetIdByCodeNameAsync(creatorTypeCodeName, ct);
        if (creatorTypeId is null)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation,
                $"CreatorType '{creatorTypeCodeName}' not found");

        var statusCodeName = string.IsNullOrWhiteSpace(command.StatusCodeName)
            ? "Active"
            : command.StatusCodeName.Trim();
        var statusId = await topicStatusesRepo.GetIdByCodeNameAsync(statusCodeName, ct);
        if (statusId is null)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation,
                $"TopicStatus '{statusCodeName}' not found");

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CreatorTypeId = creatorTypeId.Value,
            CreatedBy = createdByUserId,
            StatusId = statusId.Value,
        };

        var created = await repo.AddAsync(topic, ct);
        await repo.SaveChangesAsync(ct);

        // Загружаем DTO через GetAsync (чтобы вернуть тот же формат с навигацией)
        var dto = await repo.GetAsync(created.Id, ct);
        return dto is null
            ? Result<TopicDto, TopicsError>.Fail(TopicsError.NotFound, "Topic was created but not found")
            : Result<TopicDto, TopicsError>.Ok(dto);
    }

    /// <inheritdoc />
    public async Task<Result<TopicDto, TopicsError>> ReplaceAsync(
        Guid id, ReplaceTopicCommand command, Guid callerUserId, CancellationToken ct)
    {
        var topic = await repo.GetByIdForUpdateAsync(id, ct);
        if (topic is null)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.NotFound, "Topic not found");

        // Только автор может заменить
        if (topic.CreatedBy != callerUserId)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Forbidden, "Only the author can replace the topic");

        // Валидация — все поля обязательны
        var title = command.Title.Trim();
        if (title.Length == 0)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation, "Title is required");
        if (title.Length > 500)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation, "Title must be <= 500 characters");

        var description = string.IsNullOrWhiteSpace(command.Description)
            ? null
            : command.Description.Trim();

        var statusCodeName = command.StatusCodeName.Trim();
        if (statusCodeName.Length == 0)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation, "StatusCodeName is required");

        var statusId = await topicStatusesRepo.GetIdByCodeNameAsync(statusCodeName, ct);
        if (statusId is null)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation,
                $"TopicStatus '{statusCodeName}' not found");

        // Полная замена полей
        topic.Title = title;
        topic.Description = description;
        topic.StatusId = statusId.Value;

        await repo.SaveChangesAsync(ct);

        var dto = await repo.GetAsync(topic.Id, ct);
        return dto is not null
            ? Result<TopicDto, TopicsError>.Ok(dto)
            : Result<TopicDto, TopicsError>.Fail(TopicsError.NotFound, "Topic not found after replace");
    }

    /// <inheritdoc />
    public async Task<Result<TopicDto, TopicsError>> UpdateAsync(
        Guid id, UpdateTopicCommand command, Guid callerUserId, CancellationToken ct)
    {
        var topic = await repo.GetByIdForUpdateAsync(id, ct);
        if (topic is null)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.NotFound, "Topic not found");

        // Только автор может редактировать
        if (topic.CreatedBy != callerUserId)
            return Result<TopicDto, TopicsError>.Fail(TopicsError.Forbidden, "Only the author can update the topic");

        // Применяем изменения (null — не изменять)
        if (command.Title is not null)
        {
            var title = command.Title.Trim();
            if (title.Length == 0)
                return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation, "Title cannot be empty");
            if (title.Length > 500)
                return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation, "Title must be <= 500 characters");
            topic.Title = title;
        }

        if (command.Description is not null)
        {
            topic.Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim();
        }

        if (command.StatusCodeName is not null)
        {
            var statusCodeName = command.StatusCodeName.Trim();
            if (statusCodeName.Length == 0)
                return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation, "StatusCodeName cannot be empty");
            var statusId = await topicStatusesRepo.GetIdByCodeNameAsync(statusCodeName, ct);
            if (statusId is null)
                return Result<TopicDto, TopicsError>.Fail(TopicsError.Validation,
                    $"TopicStatus '{statusCodeName}' not found");
            topic.StatusId = statusId.Value;
        }

        // Сохраняем через EF Core (трекинг включён)
        await repo.SaveChangesAsync(ct);

        var dto = await repo.GetAsync(topic.Id, ct);
        return dto is not null
            ? Result<TopicDto, TopicsError>.Ok(dto)
            : Result<TopicDto, TopicsError>.Fail(TopicsError.NotFound, "Topic not found after update");
    }

    /// <inheritdoc />
    public async Task<Result<bool, TopicsError>> DeleteAsync(
        Guid id, Guid callerUserId, CancellationToken ct)
    {
        var topic = await repo.GetByIdForUpdateAsync(id, ct);
        if (topic is null)
            return Result<bool, TopicsError>.Fail(TopicsError.NotFound, "Topic not found");

        // Только автор может удалить
        if (topic.CreatedBy != callerUserId)
            return Result<bool, TopicsError>.Fail(TopicsError.Forbidden, "Only the author can delete the topic");

        // Нельзя удалить тему с заявками
        if (await repo.HasApplicationsAsync(id, ct))
            return Result<bool, TopicsError>.Fail(TopicsError.Validation,
                "Cannot delete a topic that has student applications");

        await repo.DeleteAsync(id, ct);
        return Result<bool, TopicsError>.Ok(true);
    }
}
