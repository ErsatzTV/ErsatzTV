using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexArtistsHandler : IRequestHandler<QuerySearchIndexArtists, ArtistCardResultsViewModel
    >
{
    private readonly IArtistRepository _artistRepository;
    private readonly ISearchIndex _searchIndex;

    public QuerySearchIndexArtistsHandler(ISearchIndex searchIndex, IArtistRepository artistRepository)
    {
        _searchIndex = searchIndex;
        _artistRepository = artistRepository;
    }

    public async Task<ArtistCardResultsViewModel> Handle(
        QuerySearchIndexArtists request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = await _searchIndex.Search(
            request.Query,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        List<ArtistCardViewModel> items = await _artistRepository
            .GetArtistsForCards(searchResult.Items.Map(i => i.Id).ToList())
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new ArtistCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
