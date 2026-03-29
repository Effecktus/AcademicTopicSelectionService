using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.StudyGroups;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Dictionaries.StudyGroups;

public sealed class StudyGroupsServiceTests
{
    private readonly IStudyGroupsRepository _repo = Substitute.For<IStudyGroupsRepository>();
    private readonly StudyGroupsService _sut;

    public StudyGroupsServiceTests()
    {
        _sut = new StudyGroupsService(_repo);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public async Task ListAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        _repo.ListAsync(Arg.Any<ListStudyGroupsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<StudyGroupDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListStudyGroupsQuery(null, inputPage, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListStudyGroupsQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(201, 200)]
    [InlineData(50, 50)]
    public async Task ListAsync_NormalizesPageSize(int inputPageSize, int expectedPageSize)
    {
        _repo.ListAsync(Arg.Any<ListStudyGroupsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<StudyGroupDto>(1, expectedPageSize, 0, []));

        await _sut.ListAsync(new ListStudyGroupsQuery(null, 1, inputPageSize), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListStudyGroupsQuery>(q => q.PageSize == expectedPageSize),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenCodeNameIsNull()
    {
        var result = await _sut.CreateAsync(new UpsertStudyGroupCommand(null), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.Validation);
        result.Message.Should().Contain("required");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(999)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public async Task CreateAsync_ReturnsValidationError_WhenCodeNameOutOfRange(int codeName)
    {
        var result = await _sut.CreateAsync(new UpsertStudyGroupCommand(codeName), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.Validation);
        result.Message.Should().Contain("1000").And.Contain("9999");
        await _repo.DidNotReceive().CreateAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(4411)]
    [InlineData(9999)]
    public async Task CreateAsync_CallsRepo_WhenCodeNameIsValid(int codeName)
    {
        var expected = MakeDto(codeName);
        _repo.ExistsByCodeNameAsync(codeName, null, Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync(codeName, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.CreateAsync(new UpsertStudyGroupCommand(codeName), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
        await _repo.Received(1).CreateAsync(codeName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenCodeNameAlreadyExists()
    {
        _repo.ExistsByCodeNameAsync(4411, null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new UpsertStudyGroupCommand(4411), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.Conflict);
        await _repo.DidNotReceive().CreateAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsValidationError_WhenCodeNameIsNull()
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpsertStudyGroupCommand(null), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.Validation);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(10000)]
    public async Task UpdateAsync_ReturnsValidationError_WhenCodeNameOutOfRange(int codeName)
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpsertStudyGroupCommand(codeName), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenCodeNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByCodeNameAsync(4411, id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(id, new UpsertStudyGroupCommand(4411), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByCodeNameAsync(4411, id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, 4411, Arg.Any<CancellationToken>()).Returns((StudyGroupDto?)null);

        var result = await _sut.UpdateAsync(id, new UpsertStudyGroupCommand(4411), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto(4411, id);
        _repo.ExistsByCodeNameAsync(4411, id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(id, 4411, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.UpdateAsync(id, new UpsertStudyGroupCommand(4411), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PatchAsync_ReturnsValidationError_WhenNoFieldsProvided()
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertStudyGroupCommand(null), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.Validation);
        result.Message.Should().Contain("At least one field");
    }

    [Theory]
    [InlineData(999)]
    [InlineData(10000)]
    public async Task PatchAsync_ReturnsValidationError_WhenCodeNameOutOfRange(int codeName)
    {
        var result = await _sut.PatchAsync(Guid.NewGuid(), new UpsertStudyGroupCommand(codeName), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.Validation);
    }

    [Fact]
    public async Task PatchAsync_ReturnsConflict_WhenCodeNameTakenByOtherRecord()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByCodeNameAsync(4411, id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.PatchAsync(id, new UpsertStudyGroupCommand(4411), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.Conflict);
    }

    [Fact]
    public async Task PatchAsync_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.ExistsByCodeNameAsync(4411, id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, 4411, Arg.Any<CancellationToken>()).Returns((StudyGroupDto?)null);

        var result = await _sut.PatchAsync(id, new UpsertStudyGroupCommand(4411), CancellationToken.None);

        result.Error.Should().Be(StudyGroupsError.NotFound);
    }

    [Fact]
    public async Task PatchAsync_ReturnsPatchedDto_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var expected = MakeDto(4411, id);
        _repo.ExistsByCodeNameAsync(4411, id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.PatchAsync(id, 4411, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.PatchAsync(id, new UpsertStudyGroupCommand(4411), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

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

    private static StudyGroupDto MakeDto(int codeName, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), codeName, DateTime.UtcNow, null);
}
