using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicDegrees;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Dictionaries.AcademicDegrees;

public sealed class AcademicDegreesServiceTests
{
    private readonly IAcademicDegreesRepository _repo = Substitute.For<IAcademicDegreesRepository>();
    private readonly AcademicDegreesService _sut;

    public AcademicDegreesServiceTests()
    {
        _sut = new AcademicDegreesService(_repo);
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
        _repo.ListAsync(Arg.Any<ListAcademicDegreesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AcademicDegreeDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListAcademicDegreesQuery(null, inputPage, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListAcademicDegreesQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(201, 200)]
    [InlineData(100, 100)]
    public async Task ListAsync_NormalizesPageSize(int inputPageSize, int expectedPageSize)
    {
        _repo.ListAsync(Arg.Any<ListAcademicDegreesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AcademicDegreeDto>(1, expectedPageSize, 0, []));

        await _sut.ListAsync(new ListAcademicDegreesQuery(null, 1, inputPageSize), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListAcademicDegreesQuery>(q => q.PageSize == expectedPageSize),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("  кандидат  ", "кандидат")]
    [InlineData("doctor", "doctor")]
    public async Task ListAsync_TrimsSearchQuery(string input, string expected)
    {
        _repo.ListAsync(Arg.Any<ListAcademicDegreesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AcademicDegreeDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListAcademicDegreesQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListAcademicDegreesQuery>(q => q.Query == expected),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public async Task ListAsync_SetsQueryToNullWhenBlank(string? input)
    {
        _repo.ListAsync(Arg.Any<ListAcademicDegreesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AcademicDegreeDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListAcademicDegreesQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListAcademicDegreesQuery>(q => q.Query == null),
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
        var result = await _sut.CreateAsync(new UpsertAcademicDegreeCommand(name, "Кандидат наук", "канд. наук"), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Validation);
        result.Message.Should().Contain("Name");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameIsEmpty(string? displayName)
    {
        var result = await _sut.CreateAsync(new UpsertAcademicDegreeCommand("Candidate", displayName, "канд. наук"), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Validation);
        result.Message.Should().Contain("DisplayName");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var longName = new string('X', 101);

        var result = await _sut.CreateAsync(new UpsertAcademicDegreeCommand("Candidate", longName, null), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Validation);
        result.Message.Should().Contain("100");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenShortNameExceeds50Chars()
    {
        var longShortName = new string('X', 51);

        var result = await _sut.CreateAsync(new UpsertAcademicDegreeCommand("Candidate", "Кандидат наук", longShortName), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Validation);
        result.Message.Should().Contain("50");
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenNameAlreadyExists()
    {
        _repo.ExistsByNameAsync("Candidate", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new UpsertAcademicDegreeCommand("Candidate", "Кандидат наук", "канд. наук"), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Conflict);
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDto_WhenDataIsValid()
    {
        var expected = MakeDto("Candidate", "Кандидат наук", "канд. наук");
        _repo.ExistsByNameAsync("Candidate", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Candidate", "Кандидат наук", "канд. наук", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.CreateAsync(new UpsertAcademicDegreeCommand("Candidate", "Кандидат наук", "канд. наук"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDto_WhenShortNameIsNull()
    {
        var expected = MakeDto("None", "Без степени", null);
        _repo.ExistsByNameAsync("None", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("None", "Без степени", null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.CreateAsync(new UpsertAcademicDegreeCommand("None", "Без степени", null), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAsync_TrimsNameAndDisplayName_BeforePassingToRepo()
    {
        var expected = MakeDto("Candidate", "Кандидат наук", "канд. наук");
        _repo.ExistsByNameAsync("Candidate", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Candidate", "Кандидат наук", "канд. наук", Arg.Any<CancellationToken>()).Returns(expected);

        await _sut.CreateAsync(new UpsertAcademicDegreeCommand("  Candidate  ", "  Кандидат наук  ", "  канд. наук  "), CancellationToken.None);

        await _repo.Received(1).CreateAsync("Candidate", "Кандидат наук", "канд. наук", Arg.Any<CancellationToken>());
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
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpsertAcademicDegreeCommand(name, "Кандидат наук", null), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Doctor", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(id, new UpsertAcademicDegreeCommand("Doctor", "Доктор наук", "д-р наук"), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Candidate", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Candidate", "Кандидат наук", null, Arg.Any<CancellationToken>()).Returns((AcademicDegreeDto?)null);

        var result = await _sut.UpdateAsync(id, new UpsertAcademicDegreeCommand("Candidate", "Кандидат наук", null), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Doctor", "Доктор наук", "д-р наук", id);
        _repo.ExistsByNameAsync("Doctor", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Doctor", "Доктор наук", "д-р наук", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.UpdateAsync(id, new UpsertAcademicDegreeCommand("Doctor", "Доктор наук", "д-р наук"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    // -------------------------------------------------------------------------
    // PatchAsync — валидация + NotFound + Conflict
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNoFieldsProvided()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertAcademicDegreeCommand(null, null, null), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Validation);
        result.Message.Should().Contain("At least one field");
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNameIsEmptyString()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertAcademicDegreeCommand("  ", "Кандидат наук", null), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var result = await _sut.PatchAsync(
            Guid.NewGuid(),
            new UpsertAcademicDegreeCommand(null, new string('X', 101), null),
            CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenShortNameExceeds50Chars()
    {
        var result = await _sut.PatchAsync(
            Guid.NewGuid(),
            new UpsertAcademicDegreeCommand(null, null, new string('X', 51)),
            CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Doctor", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.PatchAsync(id, new UpsertAcademicDegreeCommand("Doctor", null, null), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.Conflict);
    }

    [Fact]
    public async Task PatchAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync(Arg.Any<string>(), id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Doctor", null, null, Arg.Any<CancellationToken>()).Returns((AcademicDegreeDto?)null);

        var result = await _sut.PatchAsync(id, new UpsertAcademicDegreeCommand("Doctor", null, null), CancellationToken.None);

        result.Error.Should().Be(AcademicDegreesError.NotFound);
    }

    [Fact]
    public async Task PatchAsync_DoesNotCheckConflict_WhenNameIsNotProvided()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Candidate", "Обновлённая степень", "канд.", id);
        _repo.PatchAsync(id, null, "Обновлённая степень", null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertAcademicDegreeCommand(null, "Обновлённая степень", null), CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.DidNotReceive().ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAsync_ReturnsPatchedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Doctor", "Доктор наук", "д-р", id);
        _repo.ExistsByNameAsync("Doctor", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Doctor", null, "д-р", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertAcademicDegreeCommand("Doctor", null, "д-р"), CancellationToken.None);

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

    private static AcademicDegreeDto MakeDto(string name, string displayName, string? shortName, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), name, displayName, shortName, DateTime.UtcNow, null);
}
