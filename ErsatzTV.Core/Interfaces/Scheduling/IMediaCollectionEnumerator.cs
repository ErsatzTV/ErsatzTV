using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IMediaCollectionEnumerator
{
    IMediaCollectionEnumerator Clone(CollectionEnumeratorState state, CancellationToken cancellationToken);
    CollectionEnumeratorState State { get; }
    Option<MediaItem> Current { get; }
    void MoveNext();
    int Count { get; }
    Option<TimeSpan> MinimumDuration { get; }
}
