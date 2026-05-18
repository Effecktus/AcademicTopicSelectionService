using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Teachers;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Teachers;

public sealed class TeachersServiceTests
{
    private readonly ITeachersRepository _repo = Substitute.For<ITeachersRepository>();
    private readonly IUsersRepository _usersRepo = Substitute.For<IUsersRepository>();
    private readonly IStudentApplicationsRepository _appRepo = Substitute.For<IStudentApplicationsRepository>();
    private readonly IGraduateWorksRepository _gwRepo = Substitute.For<IGraduateWorksRepository>();
    private readonly TeachersService _sut;

    public TeachersServiceTests()
    {
        _sut = new TeachersService(_repo, _usersRepo, _appRepo, _gwRepo);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-2, 1)]
    [InlineData(2, 2)]
    public async Task ListAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        _repo.ListAsync(Arg.Any<ListTeachersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TeacherDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListTeachersQuery(null, inputPage, 50), "Teacher", Guid.NewGuid(), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTeachersQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(300, 200)]
    public async Task ListAsync_NormalizesPageSize(int inputPageSize, int expectedPageSize)
    {
        _repo.ListAsync(Arg.Any<ListTeachersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TeacherDto>(1, expectedPageSize, 0, []));

        await _sut.ListAsync(new ListTeachersQuery(null, 1, inputPageSize), "Teacher", Guid.NewGuid(), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTeachersQuery>(q => q.PageSize == expectedPageSize),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("  ivan  ", "ivan")]
    public async Task ListAsync_TrimsQuery(string input, string expected)
    {
        _repo.ListAsync(Arg.Any<ListTeachersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TeacherDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTeachersQuery(input, 1, 50), "Teacher", Guid.NewGuid(), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTeachersQuery>(q => q.Query == expected),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_TrimsSort()
    {
        _repo.ListAsync(Arg.Any<ListTeachersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TeacherDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTeachersQuery(null, 1, 50, "  emailAsc  "), "Teacher", Guid.NewGuid(), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTeachersQuery>(q => q.Sort == "emailAsc"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ListAsync_SetsSortToNullWhenBlank(string? input)
    {
        _repo.ListAsync(Arg.Any<ListTeachersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TeacherDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTeachersQuery(null, 1, 50, input), "Teacher", Guid.NewGuid(), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTeachersQuery>(q => q.Sort == null),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ListAsync_SetsQueryToNullWhenBlank(string? input)
    {
        _repo.ListAsync(Arg.Any<ListTeachersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TeacherDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTeachersQuery(input, 1, 50), "Teacher", Guid.NewGuid(), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTeachersQuery>(q => q.Query == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_ReturnsTeacherFromRepository()
    {
        var id = Guid.NewGuid();
        var expected = new TeacherDto(
            id,
            Guid.NewGuid(),
            "teacher@test.com",
            "Иван",
            "Петров",
            null,
            "Кафедра 01",
            5,
            0,
            0,
            new DictionaryItemRefDto(Guid.NewGuid(), "None", "Без степени"),
            new DictionaryItemRefDto(Guid.NewGuid(), "None", "Без звания"),
            new DictionaryItemRefDto(Guid.NewGuid(), "Assistant", "Ассистент"),
            DateTime.UtcNow,
            null);
        _repo.GetAsync(id, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetAsync(id, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenTeacherNotFound()
    {
        _repo.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TeacherDto?)null);

        var result = await _sut.GetAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
        await _appRepo.DidNotReceive().CountOccupiedSlotsBySupervisorAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_SetsOccupiedSlots_FromApplicationRepository()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new TeacherDto(
            id, userId, "t@test.com", "A", "B", null, null, 10, 0, null,
            new DictionaryItemRefDto(Guid.NewGuid(), "None", "None"),
            new DictionaryItemRefDto(Guid.NewGuid(), "None", "None"),
            new DictionaryItemRefDto(Guid.NewGuid(), "Assistant", "Assistant"),
            DateTime.UtcNow, null);
        _repo.GetAsync(id, Arg.Any<CancellationToken>()).Returns(dto);
        _appRepo.CountOccupiedSlotsBySupervisorAsync(userId, Arg.Any<CancellationToken>()).Returns(3);

        var result = await _sut.GetAsync(id, CancellationToken.None);

        result!.OccupiedSlotsCount.Should().Be(3);
    }

    [Fact]
    public async Task ListAsync_FiltersByStudentDepartment_WhenCallerIsStudent()
    {
        var studentUserId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        _usersRepo.GetByIdAsync(studentUserId, Arg.Any<CancellationToken>())
            .Returns(new AcademicTopicSelectionService.Domain.Entities.User
            {
                Id = studentUserId,
                DepartmentId = deptId
            });
        _repo.ListAsync(Arg.Any<ListTeachersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TeacherDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTeachersQuery(null, 1, 50), "Student", studentUserId, CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTeachersQuery>(q => q.DepartmentId == deptId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_DoesNotFilterByDepartment_WhenCallerIsTeacher()
    {
        _repo.ListAsync(Arg.Any<ListTeachersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TeacherDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTeachersQuery(null, 1, 50), "Teacher", Guid.NewGuid(), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTeachersQuery>(q => q.DepartmentId == null),
            Arg.Any<CancellationToken>());
        await _usersRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // GetGraduateWorksAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGraduateWorksAsync_DelegatesToRepository()
    {
        var teacherId = Guid.NewGuid();
        var expected = new List<TeacherGraduateWorkDto>
        {
            new(Guid.NewGuid(), "Тема 1", 2024, 5, "Иванов", "Иван", null, true, false),
            new(Guid.NewGuid(), "Тема 2", 2023, 4, "Петров", "Пётр", "Петрович", false, true),
        };
        _gwRepo.GetByTeacherIdAsync(teacherId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetGraduateWorksAsync(teacherId, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        await _gwRepo.Received(1).GetByTeacherIdAsync(teacherId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGraduateWorksAsync_ReturnsEmptyList_WhenNoWorks()
    {
        var teacherId = Guid.NewGuid();
        _gwRepo.GetByTeacherIdAsync(teacherId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.GetGraduateWorksAsync(teacherId, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
