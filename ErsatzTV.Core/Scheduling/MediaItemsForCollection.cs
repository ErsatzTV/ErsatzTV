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
            default:
                throw new ArgumentOutOfRangeException();
        }

        return result.DistinctBy(x => x.Id).ToList();
    }
}
