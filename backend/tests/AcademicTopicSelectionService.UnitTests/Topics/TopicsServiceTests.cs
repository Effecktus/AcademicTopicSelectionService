using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Topics;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Topics;

public sealed class TopicsServiceTests
{
    private readonly ITopicsRepository _repo = Substitute.For<ITopicsRepository>();
    private readonly TopicsService _sut;

    public TopicsServiceTests()
    {
        _sut = new TopicsService(_repo);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 5)]
    public async Task ListAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        _repo.ListAsync(Arg.Any<ListTopicsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopicDto>(expectedPage, 50, 0, []));

        await _sut.ListAsync(new ListTopicsQuery(null, null, null, null, null, inputPage, 50),
            CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTopicsQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_TrimsStringFilters()
    {
        _repo.ListAsync(Arg.Any<ListTopicsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopicDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTopicsQuery("  q  ", "  Active  ", null, "  Teacher  ", " titleAsc ", 1, 50),
            CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTopicsQuery>(q =>
                q.Query == "q"
                && q.StatusCodeName == "Active"
                && q.CreatorTypeCodeName == "Teacher"
                && q.Sort == "titleAsc"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_SetsBlankStringsToNull()
    {
        _repo.ListAsync(Arg.Any<ListTopicsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopicDto>(1, 50, 0, []));

        await _sut.ListAsync(new ListTopicsQuery("   ", "  ", null, "\t", "   ", 1, 50), CancellationToken.None);

        await _repo.Received(1).ListAsync(
            Arg.Is<ListTopicsQuery>(q =>
                q.Query == null
                && q.StatusCodeName == null
                && q.CreatorTypeCodeName == null
                && q.Sort == null),
            Arg.Any<CancellationToken>());
    }
}
