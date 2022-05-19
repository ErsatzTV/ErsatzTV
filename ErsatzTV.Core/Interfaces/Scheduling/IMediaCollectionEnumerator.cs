using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IMediaCollectionEnumerator
{
    CollectionEnumeratorState State { get; }
    Option<MediaItem> Current { get; }
    void MoveNext();
    Option<MediaItem> Peek(int offset);
}
