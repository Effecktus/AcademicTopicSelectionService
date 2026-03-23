using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationStatuses;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Dictionaries.ApplicationStatuses;

public sealed class ApplicationStatusesServiceTests
{
    private readonly IApplicationStatusesRepository _repo = Substitute.For<IApplicationStatusesRepository>();
    private readonly ApplicationStatusesService _sut;

    public ApplicationStatusesServiceTests()
    {
        _sut = new ApplicationStatusesService(_repo);
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
        _repo.ListAsync(Arg.Any<ListApplicationStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ApplicationStatusDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListApplicationStatusQuery(null, inputPage, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListApplicationStatusQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(201, 200)]
    [InlineData(100, 100)]
    public async Task ListAsync_NormalizesPageSize(int inputPageSize, int expectedPageSize)
    {
        _repo.ListAsync(Arg.Any<ListApplicationStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ApplicationStatusDto>(1, expectedPageSize, 0, []));

        await _sut.ListAsync(new ListApplicationStatusQuery(null, 1, inputPageSize), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListApplicationStatusQuery>(q => q.PageSize == expectedPageSize),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("  Pending  ", "Pending")]
    [InlineData("Approved", "Approved")]
    public async Task ListAsync_TrimsSearchQuery(string input, string expected)
    {
        _repo.ListAsync(Arg.Any<ListApplicationStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ApplicationStatusDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListApplicationStatusQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListApplicationStatusQuery>(q => q.Query == expected),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public async Task ListAsync_SetsQueryToNullWhenBlank(string? input)
    {
        _repo.ListAsync(Arg.Any<ListApplicationStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ApplicationStatusDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListApplicationStatusQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListApplicationStatusQuery>(q => q.Query == null),
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
        var result = await _sut.CreateAsync(new UpsetApplicationStatusCommand(name, "На рассмотрении"), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.Validation);
        result.Message.Should().Contain("Name");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameIsEmpty(string? displayName)
    {
        var result = await _sut.CreateAsync(new UpsetApplicationStatusCommand("Pending", displayName), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.Validation);
        result.Message.Should().Contain("DisplayName");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var longName = new string('X', 101);

        var result = await _sut.CreateAsync(new UpsetApplicationStatusCommand("Pending", longName), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.Validation);
        result.Message.Should().Contain("100");
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenNameAlreadyExists()
    {
        _repo.ExistsByNameAsync("Pending", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new UpsetApplicationStatusCommand("Pending", "На рассмотрении"), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.Conflict);
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDto_WhenDataIsValid()
    {
        var expected = MakeDto("Pending", "На рассмотрении");
        _repo.ExistsByNameAsync("Pending", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Pending", "На рассмотрении", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.CreateAsync(new UpsetApplicationStatusCommand("Pending", "На рассмотрении"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAsync_TrimsFields_BeforePassingToRepo()
    {
        var expected = MakeDto("Pending", "На рассмотрении");
        _repo.ExistsByNameAsync("Pending", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Pending", "На рассмотрении", Arg.Any<CancellationToken>()).Returns(expected);

        await _sut.CreateAsync(new UpsetApplicationStatusCommand("  Pending  ", "  На рассмотрении  "), CancellationToken.None);

        await _repo.Received(1).CreateAsync("Pending", "На рассмотрении", Arg.Any<CancellationToken>());
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
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpsetApplicationStatusCommand(name, "На рассмотрении"), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Approved", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(id, new UpsetApplicationStatusCommand("Approved", "Одобрено"), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Approved", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Approved", "Одобрено", Arg.Any<CancellationToken>()).Returns((ApplicationStatusDto?)null);

        var result = await _sut.UpdateAsync(id, new UpsetApplicationStatusCommand("Approved", "Одобрено"), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Approved", "Одобрено", id);
        _repo.ExistsByNameAsync("Approved", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Approved", "Одобрено", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.UpdateAsync(id, new UpsetApplicationStatusCommand("Approved", "Одобрено"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    // -------------------------------------------------------------------------
    // PatchAsync — валидация + NotFound + Conflict
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNoFieldsProvided()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsetApplicationStatusCommand(null, null), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.Validation);
        result.Message.Should().Contain("At least one field");
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNameIsEmptyString()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsetApplicationStatusCommand("  ", "Одобрено"), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var result = await _sut.PatchAsync(
            Guid.NewGuid(),
            new UpsetApplicationStatusCommand(null, new string('X', 101)),
            CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Approved", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.PatchAsync(id, new UpsetApplicationStatusCommand("Approved", null), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.Conflict);
    }

    [Fact]
    public async Task PatchAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync(Arg.Any<string>(), id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Approved", null, Arg.Any<CancellationToken>()).Returns((ApplicationStatusDto?)null);

        var result = await _sut.PatchAsync(id, new UpsetApplicationStatusCommand("Approved", null), CancellationToken.None);

        result.Error.Should().Be(ApplicationStatusesError.NotFound);
    }

    [Fact]
    public async Task PatchAsync_DoesNotCheckConflict_WhenNameIsNotProvided()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Pending", "Обновлённый статус", id);
        _repo.PatchAsync(id, null, "Обновлённый статус", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsetApplicationStatusCommand(null, "Обновлённый статус"), CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.DidNotReceive().ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAsync_ReturnsPatchedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Approved", "Одобрено", id);
        _repo.ExistsByNameAsync("Approved", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Approved", null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsetApplicationStatusCommand("Approved", null), CancellationToken.None);

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

    private static ApplicationStatusDto MakeDto(string name, string displayName, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), name, displayName, DateTime.UtcNow, null);
}
