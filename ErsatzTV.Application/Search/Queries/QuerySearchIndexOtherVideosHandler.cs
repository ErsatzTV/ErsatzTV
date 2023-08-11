using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexOtherVideosHandler : IRequestHandler<QuerySearchIndexOtherVideos,
        OtherVideoCardResultsViewModel>
{
    private readonly IClient _client;
    private readonly IOtherVideoRepository _otherVideoRepository;
    private readonly ISearchIndex _searchIndex;

    public QuerySearchIndexOtherVideosHandler(
        IClient client,
        ISearchIndex searchIndex,
        IOtherVideoRepository otherVideoRepository)
    {
        _client = client;
        _searchIndex = searchIndex;
        _otherVideoRepository = otherVideoRepository;
    }

    public async Task<OtherVideoCardResultsViewModel> Handle(
        QuerySearchIndexOtherVideos request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = await _searchIndex.Search(
            _client,
            request.Query,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        List<OtherVideoCardViewModel> items = await _otherVideoRepository
            .GetOtherVideosForCards(searchResult.Items.Map(i => i.Id).ToList())
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new OtherVideoCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
