using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicTitles;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Dictionaries.AcademicTitles;

public sealed class AcademicTitlesServiceTests
{
    private readonly IAcademicTitlesRepository _repo = Substitute.For<IAcademicTitlesRepository>();
    private readonly AcademicTitlesService _sut;

    public AcademicTitlesServiceTests()
    {
        _sut = new AcademicTitlesService(_repo);
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
        _repo.ListAsync(Arg.Any<ListAcademicTitlesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AcademicTitleDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListAcademicTitlesQuery(null, inputPage, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListAcademicTitlesQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(201, 200)]
    [InlineData(100, 100)]
    public async Task ListAsync_NormalizesPageSize(int inputPageSize, int expectedPageSize)
    {
        _repo.ListAsync(Arg.Any<ListAcademicTitlesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AcademicTitleDto>(1, expectedPageSize, 0, []));

        await _sut.ListAsync(new ListAcademicTitlesQuery(null, 1, inputPageSize), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListAcademicTitlesQuery>(q => q.PageSize == expectedPageSize),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("  доцент  ", "доцент")]
    [InlineData("Professor", "Professor")]
    public async Task ListAsync_TrimsSearchQuery(string input, string expected)
    {
        _repo.ListAsync(Arg.Any<ListAcademicTitlesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AcademicTitleDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListAcademicTitlesQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListAcademicTitlesQuery>(q => q.Query == expected),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public async Task ListAsync_SetsQueryToNullWhenBlank(string? input)
    {
        _repo.ListAsync(Arg.Any<ListAcademicTitlesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AcademicTitleDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListAcademicTitlesQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListAcademicTitlesQuery>(q => q.Query == null),
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
        var result = await _sut.CreateAsync(new UpsertAcademicTitleCommand(name, "Доцент"), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.Validation);
        result.Message.Should().Contain("Name");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameIsEmpty(string? displayName)
    {
        var result = await _sut.CreateAsync(new UpsertAcademicTitleCommand("AssociateProfessor", displayName), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.Validation);
        result.Message.Should().Contain("DisplayName");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var longName = new string('X', 101);

        var result = await _sut.CreateAsync(new UpsertAcademicTitleCommand("AssociateProfessor", longName), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.Validation);
        result.Message.Should().Contain("100");
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenNameAlreadyExists()
    {
        _repo.ExistsByNameAsync("AssociateProfessor", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new UpsertAcademicTitleCommand("AssociateProfessor", "Доцент"), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.Conflict);
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDto_WhenDataIsValid()
    {
        var expected = MakeDto("AssociateProfessor", "Доцент");
        _repo.ExistsByNameAsync("AssociateProfessor", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("AssociateProfessor", "Доцент", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.CreateAsync(new UpsertAcademicTitleCommand("AssociateProfessor", "Доцент"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAsync_TrimsNameAndDisplayName_BeforePassingToRepo()
    {
        var expected = MakeDto("AssociateProfessor", "Доцент");
        _repo.ExistsByNameAsync("AssociateProfessor", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("AssociateProfessor", "Доцент", Arg.Any<CancellationToken>()).Returns(expected);

        await _sut.CreateAsync(new UpsertAcademicTitleCommand("  AssociateProfessor  ", "  Доцент  "), CancellationToken.None);

        await _repo.Received(1).CreateAsync("AssociateProfessor", "Доцент", Arg.Any<CancellationToken>());
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
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpsertAcademicTitleCommand(name, "Доцент"), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Professor", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(id, new UpsertAcademicTitleCommand("Professor", "Профессор"), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("AssociateProfessor", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "AssociateProfessor", "Доцент", Arg.Any<CancellationToken>()).Returns((AcademicTitleDto?)null);

        var result = await _sut.UpdateAsync(id, new UpsertAcademicTitleCommand("AssociateProfessor", "Доцент"), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Professor", "Профессор", id);
        _repo.ExistsByNameAsync("Professor", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Professor", "Профессор", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.UpdateAsync(id, new UpsertAcademicTitleCommand("Professor", "Профессор"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    // -------------------------------------------------------------------------
    // PatchAsync — валидация + NotFound + Conflict
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNoFieldsProvided()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertAcademicTitleCommand(null, null), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.Validation);
        result.Message.Should().Contain("At least one field");
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNameIsEmptyString()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertAcademicTitleCommand("  ", "Доцент"), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var result = await _sut.PatchAsync(
            Guid.NewGuid(),
            new UpsertAcademicTitleCommand(null, new string('X', 101)),
            CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Professor", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.PatchAsync(id, new UpsertAcademicTitleCommand("Professor", null), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.Conflict);
    }

    [Fact]
    public async Task PatchAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync(Arg.Any<string>(), id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Professor", null, Arg.Any<CancellationToken>()).Returns((AcademicTitleDto?)null);

        var result = await _sut.PatchAsync(id, new UpsertAcademicTitleCommand("Professor", null), CancellationToken.None);

        result.Error.Should().Be(AcademicTitlesError.NotFound);
    }

    [Fact]
    public async Task PatchAsync_DoesNotCheckConflict_WhenNameIsNotProvided()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("AssociateProfessor", "Обновлённое звание", id);
        _repo.PatchAsync(id, null, "Обновлённое звание", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertAcademicTitleCommand(null, "Обновлённое звание"), CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.DidNotReceive().ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAsync_ReturnsPatchedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Professor", "Профессор", id);
        _repo.ExistsByNameAsync("Professor", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Professor", null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertAcademicTitleCommand("Professor", null), CancellationToken.None);

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

    [Fact]
    public async Task GetAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();

        var result = await _sut.GetAsync(id, CancellationToken.None);

        result.Should().BeNull();
        await _repo.Received(1).GetAsync(id, Arg.Any<CancellationToken>());
    }

    private static AcademicTitleDto MakeDto(string name, string displayName, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), name, displayName, DateTime.UtcNow, null);
}
