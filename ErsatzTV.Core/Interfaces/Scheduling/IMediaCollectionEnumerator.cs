using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Scheduling
{
    public interface IMediaCollectionEnumerator
    {
        MediaCollectionEnumeratorState State { get; }
        Option<MediaItem> Current { get; }
        void MoveNext();
    }
}
