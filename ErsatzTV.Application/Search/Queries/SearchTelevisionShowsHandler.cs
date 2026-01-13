using System.Collections.Immutable;
using System.Globalization;
using Bugsnag;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class SearchTelevisionShowsHandler(
    IClient client,
    ISearchIndex searchIndex,
    IDbContextFactory<TvContext> dbContextFactory)
    : SearchUsingSearchIndexHandler(client, searchIndex),
        IRequestHandler<SearchTelevisionShows, List<NamedMediaItemViewModel>>
{
    public async Task<List<NamedMediaItemViewModel>> Handle(
        SearchTelevisionShows request,
        CancellationToken cancellationToken)
    {
        ImmutableHashSet<int> ids = await Search(LuceneSearchIndex.ShowType, request.Query, cancellationToken);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ShowMetadata
            .TagWithCallSite()
            .AsNoTracking()
            .Where(sm => ids.Contains(sm.ShowId))
            .OrderBy(sm => sm.Title)
            .ThenBy(sm => sm.Year)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ToNamedMediaItem).ToList());
    }

    private static NamedMediaItemViewModel ToNamedMediaItem(ShowMetadata show) =>
        new(
            show.ShowId,
            $"{show.Title} ({(show.Year.HasValue ? show.Year.Value.ToString(CultureInfo.InvariantCulture) : "???")})");
}
