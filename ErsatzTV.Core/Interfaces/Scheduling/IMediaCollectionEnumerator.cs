using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IMediaCollectionEnumerator
{
    void ResetState(CollectionEnumeratorState state);
    CollectionEnumeratorState State { get; }
    Option<MediaItem> Current { get; }
    void MoveNext();
    int Count { get; }
    Option<TimeSpan> MinimumDuration { get; }
}
