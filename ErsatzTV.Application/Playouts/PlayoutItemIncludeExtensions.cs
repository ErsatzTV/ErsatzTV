using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public static class PlayoutItemIncludeExtensions
{
    public static IQueryable<PlayoutItem> IncludeAllPlayoutItemDetails(this IQueryable<PlayoutItem> query)
    {
        return query
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
            .ThenInclude(mi => (mi as RemoteStream).MediaVersions);
    }
}
