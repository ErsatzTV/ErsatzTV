using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Core.Scheduling;

public static class MediaItemsForCollection
{
    public static async Task<List<MediaItem>> Collect(
        IMediaCollectionRepository mediaCollectionRepository,
        ITelevisionRepository televisionRepository,
        IArtistRepository artistRepository,
        CollectionKey collectionKey)
    {
        switch (collectionKey.CollectionType)
        {
            case ProgramScheduleItemCollectionType.Collection:
                List<MediaItem> collectionItems =
                    await mediaCollectionRepository.GetItems(collectionKey.CollectionId ?? 0);
                return collectionItems;
            case ProgramScheduleItemCollectionType.TelevisionShow:
                List<Episode> showItems =
                    await televisionRepository.GetShowItems(collectionKey.MediaItemId ?? 0);
                return showItems.Cast<MediaItem>().ToList();
            case ProgramScheduleItemCollectionType.TelevisionSeason:
                List<Episode> seasonItems =
                    await televisionRepository.GetSeasonItems(collectionKey.MediaItemId ?? 0);
                return seasonItems.Cast<MediaItem>().ToList();
            case ProgramScheduleItemCollectionType.Artist:
                List<MusicVideo> artistItems =
                    await artistRepository.GetArtistItems(collectionKey.MediaItemId ?? 0);
                return artistItems.Cast<MediaItem>().ToList();
            case ProgramScheduleItemCollectionType.MultiCollection:
                List<MediaItem> multiCollectionItems =
                    await mediaCollectionRepository.GetMultiCollectionItems(
                        collectionKey.MultiCollectionId ?? 0);
                return multiCollectionItems;
            case ProgramScheduleItemCollectionType.SmartCollection:
                List<MediaItem> smartCollectionItems =
                    await mediaCollectionRepository.GetSmartCollectionItems(
                        collectionKey.SmartCollectionId ?? 0);
                return smartCollectionItems;
            default:
                return new List<MediaItem>();
        }
    }

}