using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Students;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Students;

public sealed class StudentsServiceTests
{
    private readonly IStudentsRepository _repo = Substitute.For<IStudentsRepository>();
    private readonly StudentsService _sut;

    public StudentsServiceTests()
    {
        _sut = new StudentsService(_repo);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(4, 4)]
    public async Task ListAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        _repo.ListAsync(Arg.Any<ListStudentsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<StudentDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListStudentsQuery(null, null, inputPage, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListStudentsQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_TrimsQuery()
    {
        _repo.ListAsync(Arg.Any<ListStudentsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<StudentDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListStudentsQuery("  petrov  ", null, 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListStudentsQuery>(q => q.Query == "petrov"),
            Arg.Any<CancellationToken>());
    }
}
