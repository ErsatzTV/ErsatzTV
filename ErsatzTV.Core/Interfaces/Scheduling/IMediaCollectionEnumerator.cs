using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IMediaCollectionEnumerator
{
    CollectionEnumeratorState State { get; }
    Option<MediaItem> Current { get; }
    Option<bool> CurrentIncludeInProgramGuide { get; }
    int Count { get; }
    Option<TimeSpan> MinimumDuration { get; }
    void ResetState(CollectionEnumeratorState state);
    void MoveNext(Option<DateTimeOffset> scheduledAt);
}
