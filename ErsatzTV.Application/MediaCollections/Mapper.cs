using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections
{
    internal static class Mapper
    {
        internal static MediaCollectionViewModel ProjectToViewModel(Collection collection) =>
            new(collection.Id, collection.Name);

        internal static MultiCollectionViewModel ProjectToViewModel(MultiCollection multiCollection) =>
            new(multiCollection.Id, multiCollection.Name);
    }
}
