using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexMusicVideosHandler : IRequestHandler<QuerySearchIndexMusicVideos, MusicVideoCardResultsViewModel
    >
{
    private readonly IMusicVideoRepository _musicVideoRepository;
    private readonly ISearchIndex _searchIndex;

    public QuerySearchIndexMusicVideosHandler(ISearchIndex searchIndex, IMusicVideoRepository musicVideoRepository)
    {
        _searchIndex = searchIndex;
        _musicVideoRepository = musicVideoRepository;
    }

    public async Task<MusicVideoCardResultsViewModel> Handle(
        QuerySearchIndexMusicVideos request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = await _searchIndex.Search(
            request.Query,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        List<MusicVideoCardViewModel> items = await _musicVideoRepository
            .GetMusicVideosForCards(searchResult.Items.Map(i => i.Id).ToList())
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new MusicVideoCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}