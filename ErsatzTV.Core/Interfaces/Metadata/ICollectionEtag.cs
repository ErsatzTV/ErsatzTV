using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ICollectionEtag
{
    string ForCollectionItems(List<MediaItem> items);
}
