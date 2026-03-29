using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Dictionaries.ApplicationActionStatuses;

public sealed class ApplicationActionStatusesServiceTests
{
    private readonly IApplicationActionStatusesRepository _repo =
        Substitute.For<IApplicationActionStatusesRepository>();

    private readonly ApplicationActionStatusesService _sut;

    public ApplicationActionStatusesServiceTests()
    {
        _sut = new ApplicationActionStatusesService(_repo);
    }

    // -------------------------------------------------------------------------
    // ListAsync — нормализация параметров
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(3, 3)]
    public async Task ListAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        _repo.ListAsync(Arg.Any<ListApplicationActionStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ApplicationActionStatusDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListApplicationActionStatusQuery(null, inputPage, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListApplicationActionStatusQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(201, 200)]
    [InlineData(50, 50)]
    public async Task ListAsync_NormalizesPageSize(int inputPageSize, int expectedPageSize)
    {
        _repo.ListAsync(Arg.Any<ListApplicationActionStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ApplicationActionStatusDto>(1, expectedPageSize, 0, []));

        await _sut.ListAsync(new ListApplicationActionStatusQuery(null, 1, inputPageSize), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListApplicationActionStatusQuery>(q => q.PageSize == expectedPageSize),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("  Pending  ", "Pending")]
    [InlineData("Approved", "Approved")]
    public async Task ListAsync_TrimsSearchQuery(string input, string expected)
    {
        _repo.ListAsync(Arg.Any<ListApplicationActionStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ApplicationActionStatusDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListApplicationActionStatusQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListApplicationActionStatusQuery>(q => q.Query == expected),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public async Task ListAsync_SetsQueryToNullWhenBlank(string? input)
    {
        _repo.ListAsync(Arg.Any<ListApplicationActionStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ApplicationActionStatusDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListApplicationActionStatusQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListApplicationActionStatusQuery>(q => q.Query == null),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // CreateAsync — валидация
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenCodeNameIsEmpty(string? codeName)
    {
        var result = await _sut.CreateAsync(
            new UpsertApplicationActionStatusCommand(codeName, "На согласовании"), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.Validation);
        result.Message.Should().Contain("CodeName");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameIsEmpty(string? displayName)
    {
        var result = await _sut.CreateAsync(
            new UpsertApplicationActionStatusCommand("Pending", displayName), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.Validation);
        result.Message.Should().Contain("DisplayName");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var longName = new string('X', 101);
        var result = await _sut.CreateAsync(
            new UpsertApplicationActionStatusCommand("Pending", longName), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.Validation);
        result.Message.Should().Contain("100");
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenCodeNameAlreadyExists()
    {
        _repo.ExistsByNameAsync("Pending", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(
            new UpsertApplicationActionStatusCommand("Pending", "На согласовании"), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.Conflict);
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDto_WhenDataIsValid()
    {
        var expected = MakeDto("Pending", "На согласовании");
        _repo.ExistsByNameAsync("Pending", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Pending", "На согласовании", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.CreateAsync(
            new UpsertApplicationActionStatusCommand("Pending", "На согласовании"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAsync_TrimsFields_BeforePassingToRepo()
    {
        var expected = MakeDto("Pending", "На согласовании");
        _repo.ExistsByNameAsync("Pending", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Pending", "На согласовании", Arg.Any<CancellationToken>()).Returns(expected);

        await _sut.CreateAsync(
            new UpsertApplicationActionStatusCommand("  Pending  ", "  На согласовании  "), CancellationToken.None);

        await _repo.Received(1).CreateAsync("Pending", "На согласовании", Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_ReturnsValidationError_WhenCodeNameIsEmpty(string? codeName)
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(),
            new UpsertApplicationActionStatusCommand(codeName, "На согласовании"), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenCodeNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Approved", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(id,
            new UpsertApplicationActionStatusCommand("Approved", "Согласовано"), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Approved", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Approved", "Согласовано", Arg.Any<CancellationToken>())
            .Returns((ApplicationActionStatusDto?)null);

        var result = await _sut.UpdateAsync(id,
            new UpsertApplicationActionStatusCommand("Approved", "Согласовано"), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Approved", "Согласовано", id);
        _repo.ExistsByNameAsync("Approved", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Approved", "Согласовано", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.UpdateAsync(id,
            new UpsertApplicationActionStatusCommand("Approved", "Согласовано"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    // -------------------------------------------------------------------------
    // PatchAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNoFieldsProvided()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(),
            new UpsertApplicationActionStatusCommand(null, null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.Validation);
        result.Message.Should().Contain("At least one field");
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenCodeNameIsEmptyString()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(),
            new UpsertApplicationActionStatusCommand("  ", "Согласовано"), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsConflict_WhenCodeNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Approved", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.PatchAsync(id,
            new UpsertApplicationActionStatusCommand("Approved", null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.Conflict);
    }

    [Fact]
    public async Task PatchAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync(Arg.Any<string>(), id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Approved", null, Arg.Any<CancellationToken>())
            .Returns((ApplicationActionStatusDto?)null);

        var result = await _sut.PatchAsync(id,
            new UpsertApplicationActionStatusCommand("Approved", null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionStatusesError.NotFound);
    }

    [Fact]
    public async Task PatchAsync_DoesNotCheckConflict_WhenCodeNameIsNotProvided()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Pending", "Обновлённый статус", id);
        _repo.PatchAsync(id, null, "Обновлённый статус", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id,
            new UpsertApplicationActionStatusCommand(null, "Обновлённый статус"), CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.DidNotReceive()
            .ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAsync_ReturnsPatchedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Approved", "Согласовано", id);
        _repo.ExistsByNameAsync("Approved", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Approved", null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id,
            new UpsertApplicationActionStatusCommand("Approved", null), CancellationToken.None);

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

    private static ApplicationActionStatusDto MakeDto(string codeName, string displayName, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), codeName, displayName, DateTime.UtcNow, null);
}
