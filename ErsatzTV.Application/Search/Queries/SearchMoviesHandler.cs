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

public class SearchMoviesHandler(
    IClient client,
    ISearchIndex searchIndex,
    IDbContextFactory<TvContext> dbContextFactory)
    : SearchUsingSearchIndexHandler(client, searchIndex), IRequestHandler<SearchMovies, List<NamedMediaItemViewModel>>
{
    public async Task<List<NamedMediaItemViewModel>> Handle(SearchMovies request, CancellationToken cancellationToken)
    {
        ImmutableHashSet<int> ids = await Search(LuceneSearchIndex.MovieType, request.Query, cancellationToken);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.MovieMetadata
            .TagWithCallSite()
            .AsNoTracking()
            .Where(mm => ids.Contains(mm.MovieId))
            .OrderBy(mm => mm.Title)
            .ThenBy(mm => mm.Year)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ToNamedMediaItem).ToList());
    }

    private static NamedMediaItemViewModel ToNamedMediaItem(MovieMetadata movie) =>
        new(
            movie.MovieId,
            $"{movie.Title} ({(movie.Year.HasValue ? movie.Year.Value.ToString(CultureInfo.InvariantCulture) : "???")})");
}
