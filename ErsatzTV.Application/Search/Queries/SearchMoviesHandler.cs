using System.Globalization;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class SearchMoviesHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<SearchMovies, List<NamedMediaItemViewModel>>
{
    public async Task<List<NamedMediaItemViewModel>> Handle(SearchMovies request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.MovieMetadata
            .AsNoTracking()
            .Where(m => EF.Functions.Like(m.Title + " " + m.Year, $"%{request.Query}%"))
            .OrderBy(m => m.Title)
            .ThenBy(m => m.Year)
            .Take(10)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ToNamedMediaItem).ToList());
    }

    private static NamedMediaItemViewModel ToNamedMediaItem(MovieMetadata movie) =>
        new(
            movie.MovieId,
            $"{movie.Title} ({(movie.Year.HasValue ? movie.Year.Value.ToString(CultureInfo.InvariantCulture) : "???")})");
}
