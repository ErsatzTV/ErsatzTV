using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections
{
    internal static class Mapper
    {
        internal static MediaCollectionViewModel ProjectToViewModel(MediaCollection mediaCollection) =>
            new(mediaCollection.Id, mediaCollection.Name);
    }
}
