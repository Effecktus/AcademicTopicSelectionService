using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Teachers;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Teachers;

public sealed class TeachersServiceTests
{
    private readonly ITeachersRepository _repo = Substitute.For<ITeachersRepository>();
    private readonly TeachersService _sut;

    public TeachersServiceTests()
    {
        _sut = new TeachersService(_repo);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-2, 1)]
    [InlineData(2, 2)]
    public async Task ListAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        _repo.ListAsync(Arg.Any<ListTeachersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TeacherDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListTeachersQuery(null, inputPage, 50), CancellationToken.None);

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

        await _sut.ListAsync(new ListTeachersQuery(null, 1, inputPageSize), CancellationToken.None);

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

        await _sut.ListAsync(new ListTeachersQuery(input, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTeachersQuery>(q => q.Query == expected),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ListAsync_SetsQueryToNullWhenBlank(string? input)
    {
        _repo.ListAsync(Arg.Any<ListTeachersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TeacherDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTeachersQuery(input, 1, 50), CancellationToken.None);

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
            5,
            new DictionaryItemRefDto(Guid.NewGuid(), "None", "Без степени"),
            new DictionaryItemRefDto(Guid.NewGuid(), "None", "Без звания"),
            new DictionaryItemRefDto(Guid.NewGuid(), "Assistant", "Ассистент"),
            DateTime.UtcNow,
            null);
        _repo.GetAsync(id, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetAsync(id, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }
}
