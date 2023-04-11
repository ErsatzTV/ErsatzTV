using Dapper;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class SearchTelevisionShowsHandler : IRequestHandler<SearchTelevisionShows, List<NamedMediaItemViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public SearchTelevisionShowsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<NamedMediaItemViewModel>> Handle(SearchTelevisionShows request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Connection.QueryAsync<TelevisionShow>(
                @"SELECT Show.Id, SM.Title, SM.Year FROM Show
                    INNER JOIN ShowMetadata SM on SM.ShowId = Show.Id
                    WHERE (SM.Title || ' ' || SM.Year) LIKE @Title
                    ORDER BY SM.Title, SM.Year
                    LIMIT 10
                    COLLATE NOCASE",
                new { Title = $"%{request.Query}%" })
            .Map(list => list.Map(ToNamedMediaItem).ToList());
    }

    private static NamedMediaItemViewModel ToNamedMediaItem(TelevisionShow show) => new(
        show.Id,
        $"{show.Title} ({(show.Year.HasValue ? show.Year.Value.ToString() : "???")})");

    public record TelevisionShow(int Id, string Title, int? Year)
    {
        public TelevisionShow() : this(default, default, default)
        {
        }
    }
}
