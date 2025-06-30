using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public record PlaylistEnumeratorCollectionKey(IMediaCollectionEnumerator Enumerator, CollectionKey CollectionKey);
