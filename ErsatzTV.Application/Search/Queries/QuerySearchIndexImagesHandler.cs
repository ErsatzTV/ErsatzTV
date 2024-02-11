using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class QuerySearchIndexImagesHandler(IClient client, ISearchIndex searchIndex, IImageRepository imageRepository)
    : IRequestHandler<QuerySearchIndexImages, ImageCardResultsViewModel>
{
    public async Task<ImageCardResultsViewModel> Handle(
        QuerySearchIndexImages request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = await searchIndex.Search(
            client,
            request.Query,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        List<ImageCardViewModel> items = await imageRepository
            .GetImagesForCards(searchResult.Items.Map(i => i.Id).ToList())
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new ImageCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
