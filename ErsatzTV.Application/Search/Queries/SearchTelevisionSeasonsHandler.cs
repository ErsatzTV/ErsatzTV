using Dapper;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class SearchTelevisionSeasonsHandler : IRequestHandler<SearchTelevisionSeasons, List<NamedMediaItemViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public SearchTelevisionSeasonsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<NamedMediaItemViewModel>> Handle(
        SearchTelevisionSeasons request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Connection.QueryAsync<TelevisionSeason>(
                @"SELECT Season.Id, SM2.Title, Season.SeasonNumber FROM Season
                    INNER JOIN SeasonMetadata SM on Season.Id = SM.SeasonId
                    INNER JOIN ShowMetadata SM2 on SM2.ShowId = Season.ShowId
                    WHERE (SM2.Title || ' ' || SM.Title) LIKE @Title
                    ORDER BY SM2.Title, Season.SeasonNumber
                    LIMIT 20
                    COLLATE NOCASE",
                new { Title = $"%{request.Query}%" })
            .Map(list => list.Map(ToNamedMediaItem).ToList());
    }

    private static NamedMediaItemViewModel ToNamedMediaItem(TelevisionSeason season) => new(
        season.Id,
        $"{ShowTitle(season)} ({SeasonTitle(season)})");

    private static string ShowTitle(TelevisionSeason season) => $"{season.Title ?? "???"}";

    private static string SeasonTitle(TelevisionSeason season) => season.SeasonNumber == 0
        ? "Specials"
        : $"Season {season.SeasonNumber}";

    public record TelevisionSeason(int Id, string Title, int SeasonNumber)
    {
        public TelevisionSeason() : this(default, default, default)
        {
        }
    }
}
