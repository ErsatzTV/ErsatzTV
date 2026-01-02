using System.Globalization;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class SearchTelevisionSeasonsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<SearchTelevisionSeasons, List<NamedMediaItemViewModel>>
{
    public async Task<List<NamedMediaItemViewModel>> Handle(
        SearchTelevisionSeasons request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await (from season in dbContext.Set<Season>()
                join seasonMetadata in dbContext.Set<SeasonMetadata>()
                    on season.Id equals seasonMetadata.SeasonId
                join showMetadata in dbContext.Set<ShowMetadata>()
                    on season.ShowId equals showMetadata.ShowId
                where EF.Functions.Like(showMetadata.Title + " " + seasonMetadata.Title, $"%{request.Query}%")
                orderby showMetadata.Title, season.SeasonNumber
                select new TelevisionSeason(season.Id, showMetadata.Title, showMetadata.Year, season.SeasonNumber))
            .Take(20)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ToNamedMediaItem).ToList());
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
