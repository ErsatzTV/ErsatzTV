using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Application.Scheduling;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
public class PreviewPlaylistPlayoutHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IMediaCollectionRepository mediaCollectionRepository,
    IPlayoutBuilder playoutBuilder)
    : IRequestHandler<PreviewPlaylistPlayout, List<PlayoutItemPreviewViewModel>>
{
    public async Task<List<PlayoutItemPreviewViewModel>> Handle(
        PreviewPlaylistPlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var playout = new Playout
        {
            Channel = new Channel(Guid.NewGuid())
            {
                Number = "1",
                Name = "Playlist Preview"
            },
            Items = [],
            ScheduleKind = PlayoutScheduleKind.Classic,
            PlayoutHistory = [],
            ProgramSchedule = new ProgramSchedule
            {
                Items = [MapToScheduleItem(request)]
            },
            ProgramScheduleAlternates = [],
            FillGroupIndices = [],
            Templates = []
        };

        var referenceData = new PlayoutReferenceData(
            playout.Channel,
            Option<Deco>.None,
            playout.Items,
            playout.Templates.ToList(),
            new ProgramSchedule(),
            playout.ProgramScheduleAlternates.ToList(),
            playout.PlayoutHistory.ToList());

        // TODO: make an explicit method to preview, this is ugly
        playoutBuilder.TrimStart = false;
        playoutBuilder.DebugPlaylist = playout.ProgramSchedule.Items[0].Playlist;
        PlayoutBuildResult result = await playoutBuilder.Build(
            playout,
            referenceData,
            PlayoutBuildMode.Reset,
            cancellationToken);

        var maxItems = 0;
        Dictionary<PlaylistItem, List<MediaItem>> map =
            await mediaCollectionRepository.GetPlaylistItemMap(playout.ProgramSchedule.Items[0].Playlist);
        foreach (PlaylistItem item in playout.ProgramSchedule.Items[0].Playlist.Items)
        {
            if (item.PlayAll)
            {
                maxItems += map[item].Count;
            }
            else
            {
                maxItems += 1;
            }
        }

        // limit preview to once through the playlist
        var onceThrough = result.AddedItems.Take(maxItems).ToList();

        // load playout item details for title
        foreach (PlayoutItem playoutItem in onceThrough)
        {
            Option<MediaItem> maybeMediaItem = await dbContext.MediaItems
                .AsNoTracking()
                .Include(mi => (mi as Movie).MovieMetadata)
                .Include(mi => (mi as Movie).MediaVersions)
                .Include(mi => (mi as MusicVideo).MusicVideoMetadata)
                .Include(mi => (mi as MusicVideo).MediaVersions)
                .Include(mi => (mi as MusicVideo).Artist)
                .ThenInclude(mm => mm.ArtistMetadata)
                .Include(mi => (mi as Episode).EpisodeMetadata)
                .Include(mi => (mi as Episode).MediaVersions)
                .Include(mi => (mi as Episode).Season)
                .ThenInclude(s => s.SeasonMetadata)
                .Include(mi => (mi as Episode).Season.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Include(mi => (mi as OtherVideo).OtherVideoMetadata)
                .Include(mi => (mi as OtherVideo).MediaVersions)
                .Include(mi => (mi as Song).SongMetadata)
                .Include(mi => (mi as Song).MediaVersions)
                .Include(mi => (mi as Image).ImageMetadata)
                .Include(mi => (mi as Image).MediaVersions)
                .SelectOneAsync(mi => mi.Id, mi => mi.Id == playoutItem.MediaItemId);

            foreach (MediaItem mediaItem in maybeMediaItem)
            {
                playoutItem.MediaItem = mediaItem;
            }
        }

        return onceThrough.OrderBy(i => i.StartOffset).Map(Scheduling.Mapper.ProjectToViewModel).ToList();
    }

    private static ProgramScheduleItemFlood MapToScheduleItem(PreviewPlaylistPlayout request) =>
        new()
        {
            CollectionType = ProgramScheduleItemCollectionType.Playlist,
            Playlist = new Playlist
            {
                Items = request.Data.Items.OrderBy(i => i.Index).Map(MapToPlaylistItem).ToList()
            },
            PlaylistId = request.Data.PlaylistId,
            PlaybackOrder = PlaybackOrder.Shuffle
        };

    private static PlaylistItem MapToPlaylistItem(ReplacePlaylistItem item) =>
        new()
        {
            Index = item.Index,
            CollectionType = item.CollectionType,
            CollectionId = item.CollectionId,
            MediaItemId = item.MediaItemId,
            MultiCollectionId = item.MultiCollectionId,
            SmartCollectionId = item.SmartCollectionId,
            PlaybackOrder = item.PlaybackOrder,
            PlayAll = item.PlayAll
        };
}
