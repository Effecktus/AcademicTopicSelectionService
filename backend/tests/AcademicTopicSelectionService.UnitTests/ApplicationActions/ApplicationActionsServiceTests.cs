using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.ApplicationActions;
using AcademicTopicSelectionService.Application.Dictionaries;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.ApplicationActions;

public sealed class ApplicationActionsServiceTests
{
    private readonly IApplicationActionsRepository _repo = Substitute.For<IApplicationActionsRepository>();
    private readonly ApplicationActionsService _sut;

    private static readonly Guid PendingStatusId = Guid.NewGuid();

    public ApplicationActionsServiceTests()
    {
        _sut = new ApplicationActionsService(_repo);
        _repo.GetActionStatusIdByCodeNameAsync("Pending", Arg.Any<CancellationToken>())
            .Returns(PendingStatusId);
    }

    // -------------------------------------------------------------------------
    // ListByApplicationAsync — нормализация
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(2, 2)]
    public async Task ListByApplicationAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        var appId = Guid.NewGuid();
        _repo.ListByApplicationAsync(Arg.Any<ListApplicationActionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ApplicationActionDto>(expectedPage, 50, 0, []));

        await _sut.ListByApplicationAsync(new ListApplicationActionsQuery(appId, inputPage, 50),
            CancellationToken.None);

        await _repo.Received(1).ListByApplicationAsync(
            Arg.Is<ListApplicationActionsQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // CreateAsync — валидация
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenApplicationIdIsEmpty()
    {
        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(Guid.Empty, Guid.NewGuid(), null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Validation);
        result.Message.Should().Contain("ApplicationId");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenResponsibleIdIsEmpty()
    {
        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(Guid.NewGuid(), Guid.Empty, null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Validation);
        result.Message.Should().Contain("ResponsibleId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenCommentIsBlankString(string comment)
    {
        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(Guid.NewGuid(), Guid.NewGuid(), comment), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Validation);
        result.Message.Should().Contain("Comment");
    }

    [Fact]
    public async Task CreateAsync_ReturnsApplicationNotFound_WhenApplicationDoesNotExist()
    {
        var appId = Guid.NewGuid();
        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, Guid.NewGuid(), null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.ApplicationNotFound);
    }

    [Fact]
    public async Task CreateAsync_ReturnsResponsibleUserNotFound_WhenUserDoesNotExist()
    {
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserExistsAsync(userId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, userId, null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.ResponsibleUserNotFound);
    }

    [Fact]
    public async Task CreateAsync_ReturnsStatusNotFound_WhenPendingStatusMissing()
    {
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserExistsAsync(userId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetActionStatusIdByCodeNameAsync("Pending", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, userId, null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.StatusNotFound);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedAction_WhenDataIsValid()
    {
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expected = MakeDto(appId, userId, PendingStatusId);

        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserExistsAsync(userId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.CreateAsync(appId, userId, PendingStatusId, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, userId, null), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAsync_TrimsComment_BeforePassingToRepo()
    {
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expected = MakeDto(appId, userId, PendingStatusId, "Комментарий");

        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserExistsAsync(userId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.CreateAsync(appId, userId, PendingStatusId, "Комментарий", Arg.Any<CancellationToken>())
            .Returns(expected);

        await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, userId, "  Комментарий  "), CancellationToken.None);

        await _repo.Received(1).CreateAsync(appId, userId, PendingStatusId, "Комментарий",
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // UpdateAsync — валидация
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ReturnsValidationError_WhenNoFieldsProvided()
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(),
            new UpdateApplicationActionCommand(null, null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Validation);
        result.Message.Should().Contain("At least one field");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_ReturnsValidationError_WhenCommentIsBlankString(string comment)
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(),
            new UpdateApplicationActionCommand(null, comment), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Validation);
        result.Message.Should().Contain("Comment");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsStatusNotFound_WhenStatusIdDoesNotExist()
    {
        var statusId = Guid.NewGuid();
        _repo.ActionStatusExistsAsync(statusId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.UpdateAsync(Guid.NewGuid(),
            new UpdateApplicationActionCommand(statusId, null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.StatusNotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenActionDoesNotExist()
    {
        var id = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        _repo.ActionStatusExistsAsync(statusId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UpdateAsync(id, statusId, null, Arg.Any<CancellationToken>())
            .Returns((ApplicationActionDto?)null);

        var result = await _sut.UpdateAsync(id,
            new UpdateApplicationActionCommand(statusId, null), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedAction_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var expected = MakeDto(Guid.NewGuid(), Guid.NewGuid(), statusId, id: id);

        _repo.ActionStatusExistsAsync(statusId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UpdateAsync(id, statusId, null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.UpdateAsync(id,
            new UpdateApplicationActionCommand(statusId, null), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenActionDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(false);

        var deleted = await _sut.DeleteAsync(id, CancellationToken.None);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenActionExists()
    {
        var id = Guid.NewGuid();
        _repo.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var deleted = await _sut.DeleteAsync(id, CancellationToken.None);

        deleted.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ApplicationActionDto MakeDto(Guid applicationId, Guid ResponsibleId, Guid statusId,
        string? comment = null, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), applicationId, ResponsibleId, statusId, "Pending", "На согласовании",
            comment, DateTime.UtcNow, null);
}
