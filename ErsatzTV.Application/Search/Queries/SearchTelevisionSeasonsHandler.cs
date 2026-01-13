using System.Collections.Immutable;
using System.Globalization;
using Bugsnag;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class SearchTelevisionSeasonsHandler(
    IClient client,
    ISearchIndex searchIndex,
    IDbContextFactory<TvContext> dbContextFactory)
    : SearchUsingSearchIndexHandler(client, searchIndex),
        IRequestHandler<SearchTelevisionSeasons, List<NamedMediaItemViewModel>>
{
    public async Task<List<NamedMediaItemViewModel>> Handle(
        SearchTelevisionSeasons request,
        CancellationToken cancellationToken)
    {
        ImmutableHashSet<int> ids = await Search(LuceneSearchIndex.SeasonType, request.Query, cancellationToken);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SeasonMetadata
            .TagWithCallSite()
            .AsNoTracking()
            .Include(s => s.Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Where(sm => ids.Contains(sm.SeasonId))
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(sm => new TelevisionSeason(
                    sm.SeasonId,
                    sm.Season.Show.ShowMetadata.HeadOrNone().Match(s => s.Title, string.Empty),
                    sm.Year,
                    sm.Season.SeasonNumber))
                .OrderBy(s => s.Title)
                .ThenBy(s => s.SeasonNumber)
                .Map(ToNamedMediaItem)
                .ToList());
    }

    private static NamedMediaItemViewModel ToNamedMediaItem(TelevisionSeason season) =>
        new(season.Id, $"{ShowTitle(season)} - {SeasonTitle(season)}");

    private static string ShowTitle(TelevisionSeason season)
    {
        string title = season.Title ?? "???";
        string year = season.Year.HasValue ? season.Year.Value.ToString(CultureInfo.InvariantCulture) : "???";
        return $"{title} ({year})";
    }

    private static string SeasonTitle(TelevisionSeason season) => season.SeasonNumber == 0
        ? "Specials"
        : $"Season {season.SeasonNumber}";

    public record TelevisionSeason(int Id, string Title, int? Year, int SeasonNumber);
}
