using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.UserRoles;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Dictionaries.UserRoles;

public sealed class UserRolesServiceTests
{
    private readonly IUserRolesRepository _repo = Substitute.For<IUserRolesRepository>();
    private readonly UserRolesService _sut;

    public UserRolesServiceTests()
    {
        _sut = new UserRolesService(_repo);
    }

    // -------------------------------------------------------------------------
    // ListAsync — нормализация параметров
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public async Task ListAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        _repo.ListAsync(Arg.Any<ListUserRolesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserRoleDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListUserRolesQuery(null, inputPage, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListUserRolesQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(201, 200)]
    [InlineData(50, 50)]
    public async Task ListAsync_NormalizesPageSize(int inputPageSize, int expectedPageSize)
    {
        _repo.ListAsync(Arg.Any<ListUserRolesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserRoleDto>(1, expectedPageSize, 0, []));

        await _sut.ListAsync(new ListUserRolesQuery(null, 1, inputPageSize), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListUserRolesQuery>(q => q.PageSize == expectedPageSize),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("  admin  ", "admin")]
    [InlineData("student", "student")]
    public async Task ListAsync_TrimsSearchQuery(string input, string expected)
    {
        _repo.ListAsync(Arg.Any<ListUserRolesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserRoleDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListUserRolesQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListUserRolesQuery>(q => q.Query == expected),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public async Task ListAsync_SetsQueryToNullWhenBlank(string? input)
    {
        _repo.ListAsync(Arg.Any<ListUserRolesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserRoleDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListUserRolesQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListUserRolesQuery>(q => q.Query == null),
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
        var result = await _sut.CreateAsync(new UpsertUserRoleCommand(name, "Студент"), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.Validation);
        result.Message.Should().Contain("Name");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameIsEmpty(string? displayName)
    {
        var result = await _sut.CreateAsync(new UpsertUserRoleCommand("Student", displayName), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.Validation);
        result.Message.Should().Contain("DisplayName");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var longName = new string('X', 101);

        var result = await _sut.CreateAsync(new UpsertUserRoleCommand("Student", longName), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.Validation);
        result.Message.Should().Contain("100");
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenNameAlreadyExists()
    {
        _repo.ExistsByNameAsync("Student", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new UpsertUserRoleCommand("Student", "Студент"), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.Conflict);
        await _repo.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDto_WhenDataIsValid()
    {
        var expected = MakeDto("Student", "Студент");
        _repo.ExistsByNameAsync("Student", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Student", "Студент", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.CreateAsync(new UpsertUserRoleCommand("Student", "Студент"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAsync_TrimsNameAndDisplayName_BeforePassingToRepo()
    {
        var expected = MakeDto("Student", "Студент");
        _repo.ExistsByNameAsync("Student", null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync("Student", "Студент", Arg.Any<CancellationToken>()).Returns(expected);

        await _sut.CreateAsync(new UpsertUserRoleCommand("  Student  ", "  Студент  "), CancellationToken.None);

        await _repo.Received(1).CreateAsync("Student", "Студент", Arg.Any<CancellationToken>());
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
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpsertUserRoleCommand(name, "Студент"), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Admin", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(id, new UpsertUserRoleCommand("Admin", "Администратор"), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Admin", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Admin", "Администратор", Arg.Any<CancellationToken>()).Returns((UserRoleDto?)null);

        var result = await _sut.UpdateAsync(id, new UpsertUserRoleCommand("Admin", "Администратор"), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Admin", "Администратор", id);
        _repo.ExistsByNameAsync("Admin", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, "Admin", "Администратор", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.UpdateAsync(id, new UpsertUserRoleCommand("Admin", "Администратор"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    // -------------------------------------------------------------------------
    // PatchAsync — валидация + NotFound + Conflict
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNoFieldsProvided()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertUserRoleCommand(null, null), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.Validation);
        result.Message.Should().Contain("At least one field");
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNameIsEmptyString()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertUserRoleCommand("  ", "Студент"), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenDisplayNameExceeds100Chars()
    {
        var result = await _sut.PatchAsync(
            Guid.NewGuid(),
            new UpsertUserRoleCommand(null, new string('X', 101)),
            CancellationToken.None);

        result.Error.Should().Be(UserRolesError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsConflict_WhenNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync("Admin", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.PatchAsync(id, new UpsertUserRoleCommand("Admin", null), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.Conflict);
    }

    [Fact]
    public async Task PatchAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByNameAsync(Arg.Any<string>(), id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Admin", null, Arg.Any<CancellationToken>()).Returns((UserRoleDto?)null);

        var result = await _sut.PatchAsync(id, new UpsertUserRoleCommand("Admin", null), CancellationToken.None);

        result.Error.Should().Be(UserRolesError.NotFound);
    }

    [Fact]
    public async Task PatchAsync_DoesNotCheckConflict_WhenNameIsNotProvided()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Student", "Обновлённый студент", id);
        _repo.PatchAsync(id, null, "Обновлённый студент", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertUserRoleCommand(null, "Обновлённый студент"), CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.DidNotReceive().ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAsync_ReturnsPatchedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto("Admin", "Администратор", id);
        _repo.ExistsByNameAsync("Admin", id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, "Admin", null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertUserRoleCommand("Admin", null), CancellationToken.None);

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

    private static UserRoleDto MakeDto(string name, string displayName, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), name, displayName, DateTime.UtcNow, null);
}
