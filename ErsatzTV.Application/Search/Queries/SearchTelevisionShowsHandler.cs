using System.Globalization;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class SearchTelevisionShowsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<SearchTelevisionShows, List<NamedMediaItemViewModel>>
{
    public async Task<List<NamedMediaItemViewModel>> Handle(
        SearchTelevisionShows request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ShowMetadata
            .AsNoTracking()
            .Where(s => EF.Functions.Like(
                EF.Functions.Collate(s.Title + " " + s.Year, TvContext.CaseInsensitiveCollation),
                $"%{request.Query}%"))
            .OrderBy(s => EF.Functions.Collate(s.Title, TvContext.CaseInsensitiveCollation))
            .ThenBy(s => s.Year)
            .Take(10)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ToNamedMediaItem).ToList());
    }

    private static NamedMediaItemViewModel ToNamedMediaItem(ShowMetadata show) =>
        new(
            show.ShowId,
            $"{show.Title} ({(show.Year.HasValue ? show.Year.Value.ToString(CultureInfo.InvariantCulture) : "???")})");
}
