using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Topics;
using AcademicTopicSelectionService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Topics;

public sealed class TopicsServiceTests
{
    private readonly ITopicsRepository _repo = Substitute.For<ITopicsRepository>();
    private readonly ITopicStatusesRepository _topicStatusesRepo = Substitute.For<ITopicStatusesRepository>();
    private readonly ITopicCreatorTypesRepository _topicCreatorTypesRepo = Substitute.For<ITopicCreatorTypesRepository>();
    private readonly TopicsService _sut;

    private static readonly Guid AuthorId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid TopicId = Guid.NewGuid();
    private static readonly Guid ActiveStatusId = Guid.NewGuid();
    private static readonly Guid TeacherCreatorTypeId = Guid.NewGuid();

    public TopicsServiceTests()
    {
        _sut = new TopicsService(_repo, _topicStatusesRepo, _topicCreatorTypesRepo);

        _topicStatusesRepo.GetIdByCodeNameAsync("Active", Arg.Any<CancellationToken>())
            .Returns(ActiveStatusId);
        _topicCreatorTypesRepo.GetIdByCodeNameAsync("Teacher", Arg.Any<CancellationToken>())
            .Returns(TeacherCreatorTypeId);
    }

    // -------------------------------------------------------------------------
    // ListAsync — нормализация
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 5)]
    public async Task ListAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        _repo.ListAsync(Arg.Any<ListTopicsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopicDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListTopicsQuery(null, null, null, null, null, inputPage, 50),
            CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTopicsQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_TrimsStringFilters()
    {
        _repo.ListAsync(Arg.Any<ListTopicsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopicDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTopicsQuery("  q  ", "  Active  ", null, "  Teacher  ", " titleAsc ", 1, 50),
            CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTopicsQuery>(q =>
                q.Query == "q"
                && q.StatusCodeName == "Active"
                && q.CreatorTypeCodeName == "Teacher"
                && q.Sort == "titleAsc"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_SetsBlankStringsToNull()
    {
        _repo.ListAsync(Arg.Any<ListTopicsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopicDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTopicsQuery("   ", "  ", null, "\t", "   ", 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTopicsQuery>(q =>
                q.Query == null
                && q.StatusCodeName == null
                && q.CreatorTypeCodeName == null
                && q.Sort == null),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // CreateAsync — валидация
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenTitleIsEmpty()
    {
        var result = await _sut.CreateAsync(
            new CreateTopicCommand("", null, "Teacher", null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("Title");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenTitleIsOnlyWhitespace()
    {
        var result = await _sut.CreateAsync(
            new CreateTopicCommand("   ", null, "Teacher", null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenTitleIsTooLong()
    {
        var longTitle = new string('A', 501);
        var result = await _sut.CreateAsync(
            new CreateTopicCommand(longTitle, null, "Teacher", null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("500");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenCreatorTypeCodeNameIsEmpty()
    {
        var result = await _sut.CreateAsync(
            new CreateTopicCommand("Test", null, "", null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("CreatorTypeCodeName");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenCreatorTypeNotFound()
    {
        _topicCreatorTypesRepo.GetIdByCodeNameAsync("Unknown", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.CreateAsync(
            new CreateTopicCommand("Test", null, "Unknown", null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("Unknown");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenStatusNotFound()
    {
        _topicStatusesRepo.GetIdByCodeNameAsync("CustomStatus", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.CreateAsync(
            new CreateTopicCommand("Test", null, "Teacher", "CustomStatus"), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("CustomStatus");
    }

    [Fact]
    public async Task CreateAsync_CreatesTopic_WhenDataIsValid()
    {
        var createdTopic = new Topic
        {
            Id = TopicId,
            Title = "Test Topic",
            Description = "Description",
            CreatorTypeId = TeacherCreatorTypeId,
            CreatedBy = AuthorId,
            StatusId = ActiveStatusId,
        };
        _repo.AddAsync(Arg.Any<Topic>(), Arg.Any<CancellationToken>()).Returns(createdTopic);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));

        var result = await _sut.CreateAsync(
            new CreateTopicCommand("Test Topic", "Description", "Teacher", null), AuthorId, CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Topic");
    }

    [Fact]
    public async Task CreateAsync_ReturnsNotFound_WhenCreatedTopicCannotBeLoaded()
    {
        var createdTopic = new Topic
        {
            Id = TopicId,
            Title = "Test Topic",
            CreatorTypeId = TeacherCreatorTypeId,
            CreatedBy = AuthorId,
            StatusId = ActiveStatusId,
        };
        _repo.AddAsync(Arg.Any<Topic>(), Arg.Any<CancellationToken>()).Returns(createdTopic);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns((TopicDto?)null);

        var result = await _sut.CreateAsync(
            new CreateTopicCommand("Test Topic", null, "Teacher", null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.NotFound);
    }

    [Fact]
    public async Task CreateAsync_TrimsTitleAndDescription()
    {
        var createdTopic = new Topic { Id = TopicId, Title = "Test", CreatorTypeId = TeacherCreatorTypeId, CreatedBy = AuthorId, StatusId = ActiveStatusId };
        _repo.AddAsync(Arg.Any<Topic>(), Arg.Any<CancellationToken>()).Returns(createdTopic);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));

        await _sut.CreateAsync(
            new CreateTopicCommand("  Trimmed Title  ", "  Trimmed Desc  ", "Teacher", null),
            AuthorId, CancellationToken.None);

        await _repo.Received(1).AddAsync(
            Arg.Is<Topic>(t => t.Title == "Trimmed Title" && t.Description == "Trimmed Desc"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_SetsNullDescription_WhenBlank()
    {
        var createdTopic = new Topic { Id = TopicId, Title = "Test", CreatorTypeId = TeacherCreatorTypeId, CreatedBy = AuthorId, StatusId = ActiveStatusId };
        _repo.AddAsync(Arg.Any<Topic>(), Arg.Any<CancellationToken>()).Returns(createdTopic);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));

        await _sut.CreateAsync(
            new CreateTopicCommand("Test", "   ", "Teacher", null), AuthorId, CancellationToken.None);

        await _repo.Received(1).AddAsync(
            Arg.Is<Topic>(t => t.Description == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_UsesDefaultStatus_WhenNotSpecified()
    {
        var createdTopic = new Topic { Id = TopicId, Title = "Test", CreatorTypeId = TeacherCreatorTypeId, CreatedBy = AuthorId, StatusId = ActiveStatusId };
        _repo.AddAsync(Arg.Any<Topic>(), Arg.Any<CancellationToken>()).Returns(createdTopic);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));

        await _sut.CreateAsync(
            new CreateTopicCommand("Test", null, "Teacher", null), AuthorId, CancellationToken.None);

        await _topicStatusesRepo.Received(1).GetIdByCodeNameAsync("Active", Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // UpdateAsync — валидация и права
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenTopicDoesNotExist()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns((Topic?)null);

        var result = await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand("New Title", null, null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsForbidden_WhenCallerIsNotAuthor()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand("New Title", null, null), OtherUserId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Forbidden);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsValidationError_WhenNewTitleIsEmpty()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand("", null, null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("Title");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsValidationError_WhenNewTitleIsTooLong()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand(new string('A', 501), null, null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
    }

    [Fact]
    public async Task UpdateAsync_OnlyChangesProvidedFields()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId);
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));

        await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand("New Title", null, null), AuthorId, CancellationToken.None);

        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        entity.Title.Should().Be("New Title");
        // Description не изменился
    }

    [Fact]
    public async Task UpdateAsync_ClearsDescription_WhenEmptyStringProvided()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId, "Old desc");
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));

        await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand(null, "", null), AuthorId, CancellationToken.None);

        entity.Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesStatus_WhenStatusCodeNameProvided()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId);
        var newStatusId = Guid.NewGuid();
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));
        _topicStatusesRepo.GetIdByCodeNameAsync("Inactive", Arg.Any<CancellationToken>()).Returns(newStatusId);

        await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand(null, null, "Inactive"), AuthorId, CancellationToken.None);

        entity.StatusId.Should().Be(newStatusId);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenTopicMissingAfterSave()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId);
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns((TopicDto?)null);

        var result = await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand("New title", null, null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsValidationError_WhenStatusNotFound()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId);
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _topicStatusesRepo.GetIdByCodeNameAsync("Unknown", Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand(null, null, "Unknown"), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
    }

    // -------------------------------------------------------------------------
    // ReplaceAsync (PUT) — валидация и права
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ReplaceAsync_ReturnsNotFound_WhenTopicDoesNotExist()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns((Topic?)null);

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("New Title", null, "Active"), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.NotFound);
    }

    [Fact]
    public async Task ReplaceAsync_ReturnsForbidden_WhenCallerIsNotAuthor()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("New Title", null, "Active"), OtherUserId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Forbidden);
    }

    [Fact]
    public async Task ReplaceAsync_ReturnsValidationError_WhenTitleIsEmpty()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("", null, "Active"), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("Title");
    }

    [Fact]
    public async Task ReplaceAsync_ReturnsValidationError_WhenTitleIsWhitespace()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("   ", null, "Active"), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("Title");
    }

    [Fact]
    public async Task ReplaceAsync_ReturnsValidationError_WhenTitleIsTooLong()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand(new string('A', 501), null, "Active"), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("500");
    }

    [Fact]
    public async Task ReplaceAsync_ReturnsValidationError_WhenStatusCodeNameIsEmpty()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("Title", null, ""), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("StatusCodeName");
    }

    [Fact]
    public async Task ReplaceAsync_ReturnsValidationError_WhenStatusCodeNameIsWhitespace()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("Title", null, "   "), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("StatusCodeName");
    }

    [Fact]
    public async Task ReplaceAsync_ReturnsValidationError_WhenStatusNotFound()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));
        _topicStatusesRepo.GetIdByCodeNameAsync("Unknown", Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("Title", null, "Unknown"), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("Unknown");
    }

    [Fact]
    public async Task ReplaceAsync_ReplacesAllFields_WhenValid()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId, "Old desc");
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));
        _topicStatusesRepo.GetIdByCodeNameAsync("Inactive", Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("  New Title  ", "  New Desc  ", "Inactive"), AuthorId, CancellationToken.None);

        result.Error.Should().BeNull();
        entity.Title.Should().Be("New Title");
        entity.Description.Should().Be("New Desc");
    }

    [Fact]
    public async Task ReplaceAsync_ReturnsNotFound_WhenTopicMissingAfterSave()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId, "Old desc");
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns((TopicDto?)null);
        _topicStatusesRepo.GetIdByCodeNameAsync("Active", Arg.Any<CancellationToken>()).Returns(ActiveStatusId);

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("New Title", "New Desc", "Active"), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.NotFound);
    }

    [Fact]
    public async Task ReplaceAsync_SetsNullDescription_WhenNull()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId, "Old desc");
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));
        _topicStatusesRepo.GetIdByCodeNameAsync("Active", Arg.Any<CancellationToken>()).Returns(ActiveStatusId);

        await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("New Title", null, "Active"), AuthorId, CancellationToken.None);

        entity.Description.Should().BeNull();
    }

    [Fact]
    public async Task ReplaceAsync_SetsNullDescription_WhenBlank()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId, "Old desc");
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));
        _topicStatusesRepo.GetIdByCodeNameAsync("Active", Arg.Any<CancellationToken>()).Returns(ActiveStatusId);

        await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand("New Title", "   ", "Active"), AuthorId, CancellationToken.None);

        entity.Description.Should().BeNull();
    }

    [Fact]
    public async Task ReplaceAsync_TitleBoundary500Chars_Succeeds()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId);
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));
        _topicStatusesRepo.GetIdByCodeNameAsync("Active", Arg.Any<CancellationToken>()).Returns(ActiveStatusId);

        var result = await _sut.ReplaceAsync(
            TopicId, new ReplaceTopicCommand(new string('A', 500), null, "Active"), AuthorId, CancellationToken.None);

        result.Error.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // UpdateAsync (PATCH) — дополнительные проверки
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ReturnsValidationError_WhenTitleIsWhitespace()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand("   ", null, null), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("Title");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsValidationError_WhenStatusCodeNameIsWhitespace()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand(null, null, "   "), AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("StatusCodeName");
    }

    [Fact]
    public async Task UpdateAsync_SetsNullDescription_WhenWhitespaceProvided()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId, "Old desc");
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));

        await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand(null, "   ", null), AuthorId, CancellationToken.None);

        entity.Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_TitleBoundary500Chars_Succeeds()
    {
        var entity = MakeTopicEntity(TopicId, AuthorId);
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));

        var result = await _sut.UpdateAsync(
            TopicId, new UpdateTopicCommand(new string('A', 500), null, null), AuthorId, CancellationToken.None);

        result.Error.Should().BeNull();
        entity.Title.Should().HaveLength(500);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenTopicDoesNotExist()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>()).Returns((Topic?)null);

        var result = await _sut.DeleteAsync(TopicId, AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsForbidden_WhenCallerIsNotAuthor()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));

        var result = await _sut.DeleteAsync(TopicId, OtherUserId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Forbidden);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsValidationError_WhenTopicHasApplications()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));
        _repo.HasApplicationsAsync(TopicId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(TopicId, AuthorId, CancellationToken.None);

        result.Error.Should().Be(TopicsError.Validation);
        result.Message.Should().Contain("applications");
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_DeletesTopic_WhenNoApplications()
    {
        _repo.GetByIdForUpdateAsync(TopicId, Arg.Any<CancellationToken>())
            .Returns(MakeTopicEntity(TopicId, AuthorId));
        _repo.HasApplicationsAsync(TopicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAsync(TopicId, AuthorId, CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeTrue();
        await _repo.Received(1).DeleteAsync(TopicId, Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Topic MakeTopicEntity(Guid id, Guid createdBy, string? description = "Desc") => new()
    {
        Id = id,
        Title = "Test Topic",
        Description = description,
        CreatorTypeId = TeacherCreatorTypeId,
        CreatedBy = createdBy,
        StatusId = ActiveStatusId,
    };

    private static TopicDto MakeTopicDto(Guid id) => new(
        id, "Test Topic", "Description",
        new DictionaryItemRefDto(ActiveStatusId, "Active", "Активна"),
        new DictionaryItemRefDto(TeacherCreatorTypeId, "Teacher", "Научный руководитель"),
        AuthorId, "author@test.com", "Ivan", "Ivanov",
        DateTime.UtcNow, null);
}
