using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Core.Scheduling;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public static class MediaItemsForCollection
{
    public static async Task<List<MediaItem>> Collect(
        IMediaCollectionRepository mediaCollectionRepository,
        ITelevisionRepository televisionRepository,
        IArtistRepository artistRepository,
        CollectionKey collectionKey)
    {
        var result = new List<MediaItem>();

        switch (collectionKey.CollectionType)
        {
            case ProgramScheduleItemCollectionType.Collection:
                result.AddRange(await mediaCollectionRepository.GetItems(collectionKey.CollectionId ?? 0));
                break;
            case ProgramScheduleItemCollectionType.TelevisionShow:
                result.AddRange(await televisionRepository.GetShowItems(collectionKey.MediaItemId ?? 0));
                break;
            case ProgramScheduleItemCollectionType.TelevisionSeason:
                result.AddRange(await televisionRepository.GetSeasonItems(collectionKey.MediaItemId ?? 0));
                break;
            case ProgramScheduleItemCollectionType.Artist:
                result.AddRange(await artistRepository.GetArtistItems(collectionKey.MediaItemId ?? 0));
                break;
            case ProgramScheduleItemCollectionType.MultiCollection:
                result.AddRange(
                    await mediaCollectionRepository.GetMultiCollectionItems(collectionKey.MultiCollectionId ?? 0));
                break;
            case ProgramScheduleItemCollectionType.SmartCollection:
                result.AddRange(
                    await mediaCollectionRepository.GetSmartCollectionItems(collectionKey.SmartCollectionId ?? 0));
                break;
            case ProgramScheduleItemCollectionType.Playlist:
                result.AddRange(
                    await mediaCollectionRepository.GetPlaylistItems(collectionKey.PlaylistId ?? 0));
                break;
            case ProgramScheduleItemCollectionType.Movie:
                foreach (int mediaItemId in Optional(collectionKey.MediaItemId))
                {
                    result.AddRange(await mediaCollectionRepository.GetMovie(mediaItemId));
                }
                break;
            case ProgramScheduleItemCollectionType.Episode:
                foreach (int mediaItemId in Optional(collectionKey.MediaItemId))
                {
                    result.AddRange(await mediaCollectionRepository.GetEpisode(mediaItemId));
                }
                break;
            case ProgramScheduleItemCollectionType.MusicVideo:
                foreach (int mediaItemId in Optional(collectionKey.MediaItemId))
                {
                    result.AddRange(await mediaCollectionRepository.GetMusicVideo(mediaItemId));
                }
                break;
            case ProgramScheduleItemCollectionType.OtherVideo:
                foreach (int mediaItemId in Optional(collectionKey.MediaItemId))
                {
                    result.AddRange(await mediaCollectionRepository.GetOtherVideo(mediaItemId));
                }
                break;
            case ProgramScheduleItemCollectionType.Song:
                foreach (int mediaItemId in Optional(collectionKey.MediaItemId))
                {
                    result.AddRange(await mediaCollectionRepository.GetSong(mediaItemId));
                }
                break;
            case ProgramScheduleItemCollectionType.Image:
                foreach (int mediaItemId in Optional(collectionKey.MediaItemId))
                {
                    result.AddRange(await mediaCollectionRepository.GetImage(mediaItemId));
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(collectionKey));
        }

        return result.DistinctBy(x => x.Id).ToList();
    }
}
