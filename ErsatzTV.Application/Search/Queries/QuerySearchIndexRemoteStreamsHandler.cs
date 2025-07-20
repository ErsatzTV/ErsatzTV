using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class QuerySearchIndexRemoteStreamsHandler(
    IClient client,
    ISearchIndex searchIndex,
    IRemoteStreamRepository imageRepository)
    : IRequestHandler<QuerySearchIndexRemoteStreams, RemoteStreamCardResultsViewModel>
{
    public async Task<RemoteStreamCardResultsViewModel> Handle(
        QuerySearchIndexRemoteStreams request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = await searchIndex.Search(
            client,
            request.Query,
            string.Empty,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        List<RemoteStreamCardViewModel> items = await imageRepository
            .GetRemoteStreamsForCards(searchResult.Items.Map(i => i.Id).ToList())
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new RemoteStreamCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
