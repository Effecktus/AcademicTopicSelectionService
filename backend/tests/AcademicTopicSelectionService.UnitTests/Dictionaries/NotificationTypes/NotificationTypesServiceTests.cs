using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.NotificationTypes;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Dictionaries.NotificationTypes;

public sealed class NotificationTypesServiceTests
{
    private readonly INotificationTypesRepository _repo = Substitute.For<INotificationTypesRepository>();
    private readonly NotificationTypesService _sut;

    public NotificationTypesServiceTests()
    {
        _sut = new NotificationTypesService(_repo);
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
        _repo.ListAsync(Arg.Any<ListNotificationTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<NotificationTypeDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListNotificationTypesQuery(null, inputPage, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListNotificationTypesQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(201, 200)]
    [InlineData(100, 100)]
    public async Task ListAsync_NormalizesPageSize(int inputPageSize, int expectedPageSize)
    {
        _repo.ListAsync(Arg.Any<ListNotificationTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<NotificationTypeDto>(1, expectedPageSize, 0, []));

        await _sut.ListAsync(new ListNotificationTypesQuery(null, 1, inputPageSize), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListNotificationTypesQuery>(q => q.PageSize == expectedPageSize),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("  info  ", "info")]
    [InlineData("warning", "warning")]
    public async Task ListAsync_TrimsSearchQuery(string input, string expected)
    {
        _repo.ListAsync(Arg.Any<ListNotificationTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<NotificationTypeDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListNotificationTypesQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListNotificationTypesQuery>(q => q.Query == expected),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public async Task ListAsync_SetsQueryToNullWhenBlank(string? input)
    {
        _repo.ListAsync(Arg.Any<ListNotificationTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<NotificationTypeDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListNotificationTypesQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListNotificationTypesQuery>(q => q.Query == null),
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
        var result = await _sut.CreateAsync(new UpsertNotificationTypeCommand(name, "Информация"), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.Validation);
        result.Message.Should().Contain("Name");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameIsEmpty(string? displayName)
    {
        var result = await _sut.CreateAsync(new UpsertNotificationTypeCommand("Info", displayName), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.Validation);
        result.Message.Should().Contain("DisplayName");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var longName = new string('X', 101);

        var result = await _sut.CreateAsync(new UpsertNotificationTypeCommand("Info", longName), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.Validation);
        result.Message.Should().Contain("100");
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenNameAlreadyExists()
    {
        _repo.ExistsByNameAsync("Info", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new UpsertNotificationTypeCommand("Info", "Информация"), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.Conflict);
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDto_WhenDataIsValid()
    {
        var expected = MakeDto("Info", "Информация");
        _repo.ExistsByNameAsync("Info", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Info", "Информация", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.CreateAsync(new UpsertNotificationTypeCommand("Info", "Информация"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAsync_TrimsNameAndDisplayName_BeforePassingToRepo()
    {
        var expected = MakeDto("Info", "Информация");
        _repo.ExistsByNameAsync("Info", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Info", "Информация", Arg.Any<CancellationToken>()).Returns(expected);

        await _sut.CreateAsync(new UpsertNotificationTypeCommand("  Info  ", "  Информация  "), CancellationToken.None);

        await _repo.Received(1).CreateAsync("Info", "Информация", Arg.Any<CancellationToken>());
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
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpsertNotificationTypeCommand(name, "Информация"), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Warning", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(id, new UpsertNotificationTypeCommand("Warning", "Предупреждение"), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Info", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Info", "Информация", Arg.Any<CancellationToken>()).Returns((NotificationTypeDto?)null);

        var result = await _sut.UpdateAsync(id, new UpsertNotificationTypeCommand("Info", "Информация"), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Warning", "Предупреждение", id);
        _repo.ExistsByNameAsync("Warning", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Warning", "Предупреждение", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.UpdateAsync(id, new UpsertNotificationTypeCommand("Warning", "Предупреждение"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    // -------------------------------------------------------------------------
    // PatchAsync — валидация + NotFound + Conflict
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNoFieldsProvided()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertNotificationTypeCommand(null, null), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.Validation);
        result.Message.Should().Contain("At least one field");
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNameIsEmptyString()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertNotificationTypeCommand("  ", "Информация"), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var result = await _sut.PatchAsync(
            Guid.NewGuid(),
            new UpsertNotificationTypeCommand(null, new string('X', 101)),
            CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Warning", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.PatchAsync(id, new UpsertNotificationTypeCommand("Warning", null), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.Conflict);
    }

    [Fact]
    public async Task PatchAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync(Arg.Any<string>(), id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Warning", null, Arg.Any<CancellationToken>()).Returns((NotificationTypeDto?)null);

        var result = await _sut.PatchAsync(id, new UpsertNotificationTypeCommand("Warning", null), CancellationToken.None);

        result.Error.Should().Be(NotificationTypesError.NotFound);
    }

    [Fact]
    public async Task PatchAsync_DoesNotCheckConflict_WhenNameIsNotProvided()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Info", "Обновлённый тип", id);
        _repo.PatchAsync(id, null, "Обновлённый тип", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertNotificationTypeCommand(null, "Обновлённый тип"), CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.DidNotReceive().ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAsync_ReturnsPatchedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Warning", "Предупреждение", id);
        _repo.ExistsByNameAsync("Warning", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Warning", null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertNotificationTypeCommand("Warning", null), CancellationToken.None);

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

    private static NotificationTypeDto MakeDto(string name, string displayName, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), name, displayName, DateTime.UtcNow, null);
}
