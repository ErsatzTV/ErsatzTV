using System.Linq;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections
{
    internal static class Mapper
    {
        internal static MediaCollectionViewModel ProjectToViewModel(Collection collection) =>
            new(collection.Id, collection.Name, collection.UseCustomPlaybackOrder);

        internal static MultiCollectionViewModel ProjectToViewModel(MultiCollection multiCollection) =>
            new(
                multiCollection.Id,
                multiCollection.Name,
                Optional(multiCollection.MultiCollectionItems).Flatten().Map(ProjectToViewModel).ToList());

        private static MultiCollectionItemViewModel ProjectToViewModel(MultiCollectionItem multiCollectionItem) =>
            new(
                multiCollectionItem.MultiCollectionId,
                ProjectToViewModel(multiCollectionItem.Collection),
                multiCollectionItem.ScheduleAsGroup,
                multiCollectionItem.PlaybackOrder);
    }
}
