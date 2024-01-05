using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;

namespace ErsatzTV.Core.Scheduling;

public class CollectionKey : Record<CollectionKey>
{
    public ProgramScheduleItemCollectionType CollectionType { get; set; }
    public int? CollectionId { get; set; }
    public int? MultiCollectionId { get; set; }
    public int? SmartCollectionId { get; set; }
    public int? MediaItemId { get; set; }
    public string FakeCollectionKey { get; set; }

    public static CollectionKey ForScheduleItem(ProgramScheduleItem item) =>
        item.CollectionType switch
        {
            ProgramScheduleItemCollectionType.Collection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId
            },
            ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.Artist => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MediaItemId = item.MediaItemId
            },
            ProgramScheduleItemCollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                MultiCollectionId = item.MultiCollectionId
            },
            ProgramScheduleItemCollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = item.CollectionType,
                SmartCollectionId = item.SmartCollectionId
            },
            ProgramScheduleItemCollectionType.FakeCollection => new CollectionKey
            {
              CollectionType = item.CollectionType,
              FakeCollectionKey = item.FakeCollectionKey
            },
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        };

    public static CollectionKey ForFillerPreset(FillerPreset filler) =>
        filler.CollectionType switch
        {
            ProgramScheduleItemCollectionType.Collection => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                CollectionId = filler.CollectionId
            },
            ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MediaItemId = filler.MediaItemId
            },
            ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MediaItemId = filler.MediaItemId
            },
            ProgramScheduleItemCollectionType.Artist => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MediaItemId = filler.MediaItemId
            },
            ProgramScheduleItemCollectionType.MultiCollection => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                MultiCollectionId = filler.MultiCollectionId
            },
            ProgramScheduleItemCollectionType.SmartCollection => new CollectionKey
            {
                CollectionType = filler.CollectionType,
                SmartCollectionId = filler.SmartCollectionId
            },
            _ => throw new ArgumentOutOfRangeException(nameof(filler))
        };
}
