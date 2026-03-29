using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.TopicCreatorTypes;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Dictionaries.TopicCreatorTypes;

public sealed class TopicCreatorTypesServiceTests
{
    private readonly ITopicCreatorTypesRepository _repo = Substitute.For<ITopicCreatorTypesRepository>();
    private readonly TopicCreatorTypesService _sut;

    public TopicCreatorTypesServiceTests()
    {
        _sut = new TopicCreatorTypesService(_repo);
    }

    // -------------------------------------------------------------------------
    // ListAsync — нормализация параметров
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(5, 5)]
    public async Task ListAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        _repo.ListAsync(Arg.Any<ListTopicCreatorTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopicCreatorTypeDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListTopicCreatorTypesQuery(null, inputPage, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTopicCreatorTypesQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(201, 200)]
    [InlineData(100, 100)]
    public async Task ListAsync_NormalizesPageSize(int inputPageSize, int expectedPageSize)
    {
        _repo.ListAsync(Arg.Any<ListTopicCreatorTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopicCreatorTypeDto>(1, expectedPageSize, 0, []));

        await _sut.ListAsync(new ListTopicCreatorTypesQuery(null, 1, inputPageSize), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTopicCreatorTypesQuery>(q => q.PageSize == expectedPageSize),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("  teacher  ", "teacher")]
    [InlineData("student", "student")]
    public async Task ListAsync_TrimsSearchQuery(string input, string expected)
    {
        _repo.ListAsync(Arg.Any<ListTopicCreatorTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopicCreatorTypeDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTopicCreatorTypesQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTopicCreatorTypesQuery>(q => q.Query == expected),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public async Task ListAsync_SetsQueryToNullWhenBlank(string? input)
    {
        _repo.ListAsync(Arg.Any<ListTopicCreatorTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopicCreatorTypeDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTopicCreatorTypesQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTopicCreatorTypesQuery>(q => q.Query == null),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // CreateAsync — валидация
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenNameIsEmpty(string? name)
    {
        var result = await _sut.CreateAsync(new UpsertTopicCreatorTypeCommand(name, "Научный руководитель"), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.Validation);
        result.Message.Should().Contain("Name");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameIsEmpty(string? displayName)
    {
        var result = await _sut.CreateAsync(new UpsertTopicCreatorTypeCommand("Teacher", displayName), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.Validation);
        result.Message.Should().Contain("DisplayName");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var longName = new string('X', 101);

        var result = await _sut.CreateAsync(new UpsertTopicCreatorTypeCommand("Teacher", longName), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.Validation);
        result.Message.Should().Contain("100");
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenNameAlreadyExists()
    {
        _repo.ExistsByNameAsync("Teacher", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new UpsertTopicCreatorTypeCommand("Teacher", "Научный руководитель"), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.Conflict);
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDto_WhenDataIsValid()
    {
        var expected = MakeDto("Teacher", "Научный руководитель");
        _repo.ExistsByNameAsync("Teacher", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Teacher", "Научный руководитель", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.CreateAsync(new UpsertTopicCreatorTypeCommand("Teacher", "Научный руководитель"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAsync_TrimsNameAndDisplayName_BeforePassingToRepo()
    {
        var expected = MakeDto("Teacher", "Научный руководитель");
        _repo.ExistsByNameAsync("Teacher", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Teacher", "Научный руководитель", Arg.Any<CancellationToken>()).Returns(expected);

        await _sut.CreateAsync(new UpsertTopicCreatorTypeCommand("  Teacher  ", "  Научный руководитель  "), CancellationToken.None);

        await _repo.Received(1).CreateAsync("Teacher", "Научный руководитель", Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // UpdateAsync — валидация + NotFound + Conflict
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_ReturnsValidationError_WhenNameIsEmpty(string? name)
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpsertTopicCreatorTypeCommand(name, "Научный руководитель"), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Student", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(id, new UpsertTopicCreatorTypeCommand("Student", "Студент"), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Student", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Student", "Студент", Arg.Any<CancellationToken>()).Returns((TopicCreatorTypeDto?)null);

        var result = await _sut.UpdateAsync(id, new UpsertTopicCreatorTypeCommand("Student", "Студент"), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Student", "Студент", id);
        _repo.ExistsByNameAsync("Student", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Student", "Студент", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.UpdateAsync(id, new UpsertTopicCreatorTypeCommand("Student", "Студент"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    // -------------------------------------------------------------------------
    // PatchAsync — валидация + NotFound + Conflict
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNoFieldsProvided()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertTopicCreatorTypeCommand(null, null), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.Validation);
        result.Message.Should().Contain("At least one field");
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNameIsEmptyString()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertTopicCreatorTypeCommand("  ", "Научный руководитель"), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var result = await _sut.PatchAsync(
            Guid.NewGuid(),
            new UpsertTopicCreatorTypeCommand(null, new string('X', 101)),
            CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Student", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.PatchAsync(id, new UpsertTopicCreatorTypeCommand("Student", null), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.Conflict);
    }

    [Fact]
    public async Task PatchAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync(Arg.Any<string>(), id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Student", null, Arg.Any<CancellationToken>()).Returns((TopicCreatorTypeDto?)null);

        var result = await _sut.PatchAsync(id, new UpsertTopicCreatorTypeCommand("Student", null), CancellationToken.None);

        result.Error.Should().Be(TopicCreatorTypesError.NotFound);
    }

    [Fact]
    public async Task PatchAsync_DoesNotCheckConflict_WhenNameIsNotProvided()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Teacher", "Обновлённый тип", id);
        _repo.PatchAsync(id, null, "Обновлённый тип", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertTopicCreatorTypeCommand(null, "Обновлённый тип"), CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.DidNotReceive().ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAsync_ReturnsPatchedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Student", "Студент", id);
        _repo.ExistsByNameAsync("Student", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Student", null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertTopicCreatorTypeCommand("Student", null), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(false);

        var deleted = await _sut.DeleteAsync(id, CancellationToken.None);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenRecordExists()
    {
        var id = Guid.NewGuid();
        _repo.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var deleted = await _sut.DeleteAsync(id, CancellationToken.None);

        deleted.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static TopicCreatorTypeDto MakeDto(string name, string displayName, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), name, displayName, DateTime.UtcNow, null);
}
