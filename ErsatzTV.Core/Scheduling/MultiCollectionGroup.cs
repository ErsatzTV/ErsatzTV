using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling;

public class MultiCollectionGroup : GroupedMediaItem
{
    public MultiCollectionGroup(CollectionWithItems collectionWithItems)
    {
        if (collectionWithItems.UseCustomOrder)
        {
            if (collectionWithItems.MediaItems.Count > 0)
            {
                First = collectionWithItems.MediaItems.Head();
                Additional = collectionWithItems.MediaItems.Tail().ToList();
            }
            else
            {
                throw new InvalidOperationException("Collection has no items");
            }
        }
        else
        {
            switch (collectionWithItems.PlaybackOrder)
            {
                case PlaybackOrder.Chronological:
                {
                    var sortedItems = collectionWithItems.MediaItems.OrderBy(identity, new ChronologicalMediaComparer())
                        .ToList();
                    First = sortedItems.Head();
                    Additional = sortedItems.Tail().ToList();
                }
                    break;
                case PlaybackOrder.SeasonEpisode:
                {
                    var sortedItems = collectionWithItems.MediaItems.OrderBy(identity, new SeasonEpisodeMediaComparer())
                        .ToList();
                    First = sortedItems.Head();
                    Additional = sortedItems.Tail().ToList();
                }
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unsupported MultiCollection PlaybackOrder: {collectionWithItems.PlaybackOrder}");
            }
        }
    }
}
