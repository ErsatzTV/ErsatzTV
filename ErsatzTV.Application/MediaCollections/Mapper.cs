using ErsatzTV.Core.AggregateModels;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections
{
    internal static class Mapper
    {
        internal static MediaCollectionViewModel ProjectToViewModel(MediaCollection mediaCollection) =>
            new(mediaCollection.Id, mediaCollection.Name);

        internal static MediaCollectionSummaryViewModel ProjectToViewModel(
            MediaCollectionSummary mediaCollectionSummary) =>
            new(
                mediaCollectionSummary.Id,
                mediaCollectionSummary.Name,
                mediaCollectionSummary.ItemCount,
                mediaCollectionSummary.IsSimple);
    }
}
