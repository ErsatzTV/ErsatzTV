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

        IQueryable<PlayoutItem> query = dbContext.PlayoutItems
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
            .Filter(i => i.PlayoutId == request.PlayoutId)
            .Filter(i => i.Finish >= now);

        if (request.ShowFiller)
        {
            List<PlayoutItemViewModel> allItems = await query
                .OrderBy(i => i.Start)
                .ToListAsync(cancellationToken)
                .Map(list => list.Map(ProjectToViewModel).ToList());
            List<PlayoutItemViewModel> withGaps = InsertUnscheduledItems(allItems);
            int totalCount = withGaps.Count;
            List<PlayoutItemViewModel> finalPage =
                withGaps.Skip(request.PageNum * request.PageSize).Take(request.PageSize).ToList();
            return new PagedPlayoutItemsViewModel(totalCount, finalPage);
        }
        else
        {
            int totalCount = await dbContext.PlayoutItems
                .CountAsync(
                    i => i.Finish >= now && i.PlayoutId == request.PlayoutId &&
                         i.FillerKind == FillerKind.None,
                    cancellationToken);
            List<PlayoutItemViewModel> page = await query
                .Filter(i => i.FillerKind == FillerKind.None)
                .OrderBy(i => i.Start)
                .Skip(request.PageNum * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken)
                .Map(list => list.Map(ProjectToViewModel).ToList());
            return new PagedPlayoutItemsViewModel(totalCount, page);
        }
    }

    private static List<PlayoutItemViewModel> InsertUnscheduledItems(List<PlayoutItemViewModel> items)
    {
        List<PlayoutItemViewModel> result = [];
        PlayoutItemViewModel prev = null;
        foreach (PlayoutItemViewModel item in items)
        {
            if (prev != null && (item.Start - prev.Finish).TotalSeconds > 2)
            {
                TimeSpan gapDuration = item.Start - prev.Finish;
                result.Add(
                    new PlayoutItemViewModel(
                        "UNSCHEDULED",
                        prev.Finish,
                        item.Start,
                        TimeSpan.FromSeconds(Math.Round(gapDuration.TotalSeconds)).ToString(
                            gapDuration.TotalHours >= 1 ? @"h\:mm\:ss" : @"mm\:ss",
                            CultureInfo.CurrentUICulture.DateTimeFormat),
                        None
                    ));
            }

            result.Add(item);
            prev = item;
        }

        return result;
    }
}
