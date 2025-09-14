using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Playouts.Mapper;

namespace ErsatzTV.Application.Playouts;

public class GetFuturePlayoutItemsByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetFuturePlayoutItemsById, PagedPlayoutItemsViewModel>
{
    public async Task<PagedPlayoutItemsViewModel> Handle(
        GetFuturePlayoutItemsById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        DateTime now = DateTimeOffset.Now.UtcDateTime;

        // items for total count
        var items = dbContext.PlayoutItems
            .AsNoTracking()
            .Filter(i => i.PlayoutId == request.PlayoutId)
            .Filter(i => i.Finish >= now)
            .Filter(i => request.ShowFiller || i.FillerKind == FillerKind.None)
            .Select(i => new { i.Id, Type = "Item", i.Start, i.Finish });

        // gaps for total count
        var gaps = dbContext.PlayoutGaps
            .AsNoTracking()
            .Filter(g => g.PlayoutId == request.PlayoutId)
            .Filter(g => g.Finish >= now)
            .Select(g => new { g.Id, Type = "Gap", g.Start, g.Finish });

        var combined = items.Concat(gaps);

        int totalCount = await combined.CountAsync(cancellationToken);

        List<PlayoutItemViewModel> page = await combined
            .OrderBy(c => c.Start)
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .Bind(async pageOfCombined =>
            {
                var itemIds = pageOfCombined.Where(i => i.Type == "Item").Select(i => i.Id).ToList();
                var gapIds = pageOfCombined.Where(i => i.Type == "Gap").Select(i => i.Id).ToList();

                // full playout items
                List<PlayoutItem> playoutItems = await dbContext.PlayoutItems
                    .AsNoTracking()
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as Movie).MovieMetadata)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as Movie).MediaVersions)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as MusicVideo).MusicVideoMetadata)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as MusicVideo).MediaVersions)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as MusicVideo).Artist)
                    .ThenInclude(mm => mm.ArtistMetadata)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as Episode).EpisodeMetadata)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as Episode).MediaVersions)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as Episode).Season)
                    .ThenInclude(s => s.SeasonMetadata)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as Episode).Season.Show)
                    .ThenInclude(s => s.ShowMetadata)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as OtherVideo).OtherVideoMetadata)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as OtherVideo).MediaVersions)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as Song).SongMetadata)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as Song).MediaVersions)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as Image).ImageMetadata)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as Image).MediaVersions)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as RemoteStream).RemoteStreamMetadata)
                    .Include(i => i.MediaItem)
                    .ThenInclude(mi => (mi as RemoteStream).MediaVersions)
                    .Where(i => itemIds.Contains(i.Id))
                    .ToListAsync(cancellationToken);

                // full gaps
                List<PlayoutGap> playoutGaps = await dbContext.PlayoutGaps
                    .AsNoTracking()
                    .Where(g => gapIds.Contains(g.Id))
                    .ToListAsync(cancellationToken);

                return pageOfCombined.Select(c =>
                {
                    if (c.Type == "Item")
                    {
                        var item = playoutItems.Single(i => i.Id == c.Id);
                        return ProjectToViewModel(item);
                    }

                    var gap = playoutGaps.Single(g => g.Id == c.Id);
                    TimeSpan gapDuration = gap.Finish - gap.Start;
                    return new PlayoutItemViewModel(
                        "UNSCHEDULED",
                        gap.StartOffset,
                        gap.FinishOffset,
                        TimeSpan.FromSeconds(Math.Round(gapDuration.TotalSeconds)).ToString(
                            gapDuration.TotalHours >= 1 ? @"h\:mm\:ss" : @"mm\:ss",
                            CultureInfo.CurrentUICulture.DateTimeFormat),
                        None
                    );
                }).ToList();
            });

        return new PagedPlayoutItemsViewModel(totalCount, page);
    }
}
