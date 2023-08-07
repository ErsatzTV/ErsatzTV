using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IMediaCollectionEnumerator
{
    CollectionEnumeratorState State { get; }
    Option<MediaItem> Current { get; }
    int Count { get; }
    Option<TimeSpan> MinimumDuration { get; }
    void ResetState(CollectionEnumeratorState state);
    void MoveNext();
}
