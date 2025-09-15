using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Infrastructure.Scheduling;

public class RerunMediaCollectionEnumerator : IMediaCollectionEnumerator
{
    private readonly CollectionKey _collectionKey;
    private readonly IRerunHelper _rerunHelper;
    private IMediaCollectionEnumerator _enumerator;

    private RerunMediaCollectionEnumerator(IRerunHelper rerunHelper, CollectionKey collectionKey)
    {
        _collectionKey = collectionKey;
        _rerunHelper = rerunHelper;
    }

    public CollectionEnumeratorState State => _enumerator.State;
    public Option<MediaItem> Current => _enumerator.Current;
    public Option<bool> CurrentIncludeInProgramGuide => _enumerator.CurrentIncludeInProgramGuide;
    public int Count => _enumerator.Count;
    public Option<TimeSpan> MinimumDuration => _enumerator.MinimumDuration;

    public void ResetState(CollectionEnumeratorState state)
    {
        _enumerator.ResetState(state);
        MoveToNextValid();
    }

    public void MoveNext()
    {
        // TODO: do we need this async so it can write to the db? how do we give it the opportunity to do that?
        if (_collectionKey.CollectionType is CollectionType.RerunFirstRun)
        {
            foreach (var current in Current)
            {
                Console.WriteLine($"adding {current.Id} to history");
                _rerunHelper.AddToHistory(_collectionKey, current.Id);
            }
        }

        _enumerator.MoveNext();
        MoveToNextValid();
    }

    private void MoveToNextValid()
    {
        switch (_collectionKey.CollectionType)
        {
            // skip to the next first run
            case CollectionType.RerunFirstRun when _rerunHelper.FirstRunCount(_collectionKey) > 0:
            {
                while (_enumerator.Current.Match(current => _rerunHelper.IsRerun(_collectionKey, current.Id), false))
                {
                    _enumerator.MoveNext();
                }

                break;
            }

            // skip to the next rerun
            case CollectionType.RerunRerun when _rerunHelper.RerunCount(_collectionKey) > 0:
            {
                while (_enumerator.Current.Match(current => _rerunHelper.IsFirstRun(_collectionKey, current.Id), false))
                {
                    _enumerator.MoveNext();
                }

                break;
            }
        }
    }

    public static RerunMediaCollectionEnumerator Create(
        IRerunHelper rerunHelper,
        CollectionKey collectionKey,
        List<MediaItem> mediaItems,
        PlaybackOrder playbackOrder,
        CollectionEnumeratorState state)
    {
        // TODO: proper enumerator based on playback order
        IMediaCollectionEnumerator enumerator = playbackOrder switch
        {
            PlaybackOrder.Chronological => new ChronologicalMediaCollectionEnumerator(mediaItems, state),
            PlaybackOrder.SeasonEpisode => new SeasonEpisodeMediaCollectionEnumerator(mediaItems, state),
            _ => new RandomizedMediaCollectionEnumerator(mediaItems, state)
        };

        return new RerunMediaCollectionEnumerator(rerunHelper, collectionKey)
        {
            _enumerator = enumerator
        };
    }
}
