using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public class SingleMediaItemEnumerator(MediaItem mediaItem) : IMediaCollectionEnumerator
{
    public CollectionEnumeratorState State { get; } = new();
    public Option<MediaItem> Current => mediaItem;
    public Option<bool> CurrentIncludeInProgramGuide => Option<bool>.None;

    public int Count => 1;
    public Option<TimeSpan> MinimumDuration => mediaItem.GetNonZeroDuration();

    public void ResetState(CollectionEnumeratorState state)
    {
        // do nothing
    }

    public void MoveNext()
    {
        // do nothing
    }
}
