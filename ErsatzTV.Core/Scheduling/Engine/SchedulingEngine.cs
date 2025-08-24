using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.BlockScheduling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Scheduling.Engine;

public class SchedulingEngine(IMediaCollectionRepository mediaCollectionRepository, ILogger<SchedulingEngine> logger)
    : ISchedulingEngine
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly Dictionary<string, EnumeratorDetails> _enumerators = new();
    private readonly SchedulingEngineState _state = new(0);
    private PlayoutReferenceData _referenceData;

    public ISchedulingEngine WithPlayoutId(int playoutId)
    {
        _state.PlayoutId = playoutId;
        return this;
    }

    public ISchedulingEngine WithMode(PlayoutBuildMode mode)
    {
        _state.Mode = mode;
        return this;
    }

    public ISchedulingEngine WithSeed(int seed)
    {
        _state.Seed = seed;
        return this;
    }

    public ISchedulingEngine WithReferenceData(PlayoutReferenceData referenceData)
    {
        _referenceData = referenceData;
        return this;
    }

    public ISchedulingEngine BuildBetween(DateTimeOffset start, DateTimeOffset finish)
    {
        _state.Start = start;
        _state.Finish = finish;
        _state.CurrentTime = start;
        return this;
    }

    public ISchedulingEngine RemoveBefore(DateTimeOffset removeBefore)
    {
        _state.RemoveBefore = removeBefore;
        return this;
    }

    public ISchedulingEngine RestoreOrReset(Option<PlayoutAnchor> maybeAnchor)
    {
        if (_state.Mode is PlayoutBuildMode.Reset)
        {
            // erase items, not history
            _state.ClearItems = true;

            // remove any future or "currently active" history items
            // this prevents "walking" the playout forward by repeatedly resetting
            var toRemove = new List<PlayoutHistory>();
            toRemove.AddRange(
                _referenceData.PlayoutHistory.Filter(h =>
                    h.When > _state.Start.UtcDateTime ||
                    h.When <= _state.Start.UtcDateTime && h.Finish >= _state.Start.UtcDateTime));
            foreach (PlayoutHistory history in toRemove)
            {
                _state.HistoryToRemove.Add(history.Id);
            }
        }
        else
        {
            foreach (PlayoutAnchor anchor in maybeAnchor)
            {
                _state.CurrentTime = new DateTimeOffset(anchor.NextStart.ToLocalTime(), _state.CurrentTime.Offset);

                if (string.IsNullOrWhiteSpace(anchor.Context))
                {
                    break;
                }

                SerializedState state = JsonConvert.DeserializeObject<SerializedState>(anchor.Context);
                _state.LoadContext(state);
            }
        }

        return this;
    }

    public async Task<ISchedulingEngine> AddCollection(string key, string collectionName, PlaybackOrder playbackOrder)
    {
        if (!_enumerators.ContainsKey(key))
        {
            int index = _enumerators.Count;
            List<MediaItem> items = await mediaCollectionRepository.GetCollectionItemsByName(collectionName);
            if (items.Count == 0)
            {
                logger.LogWarning("Skipping invalid or empty collection {Name}", collectionName);
                return this;
            }

            var state = new CollectionEnumeratorState { Seed = _state.Seed + index, Index = 0 };
            foreach (var enumerator in EnumeratorForContent(items, state, playbackOrder))
            {
                string historyKey = HistoryDetails.KeyForSchedulingContent(key, playbackOrder);
                var details = new EnumeratorDetails(enumerator, historyKey, playbackOrder);

                if (_enumerators.TryAdd(key, details))
                {
                    logger.LogDebug(
                        "Added collection {Name} with key {Key} and order {Order}",
                        collectionName,
                        key,
                        playbackOrder);

                    ApplyHistory(historyKey, items, enumerator, playbackOrder);
                }
            }
        }

        return this;
    }

    public async Task<ISchedulingEngine> AddMultiCollection(
        string key,
        string multiCollectionName,
        PlaybackOrder playbackOrder)
    {
        if (!_enumerators.ContainsKey(key))
        {
            int index = _enumerators.Count;
            List<MediaItem> items = await mediaCollectionRepository.GetMultiCollectionItemsByName(multiCollectionName);
            if (items.Count == 0)
            {
                logger.LogWarning("Skipping invalid or empty multi collection {Name}", multiCollectionName);
                return this;
            }

            var state = new CollectionEnumeratorState { Seed = _state.Seed + index, Index = 0 };
            foreach (var enumerator in EnumeratorForContent(items, state, playbackOrder))
            {
                string historyKey = HistoryDetails.KeyForSchedulingContent(key, playbackOrder);
                var details = new EnumeratorDetails(enumerator, historyKey, playbackOrder);

                if (_enumerators.TryAdd(key, details))
                {
                    logger.LogDebug(
                        "Added multi collection {Name} with key {Key} and order {Order}",
                        multiCollectionName,
                        key,
                        playbackOrder);

                    ApplyHistory(historyKey, items, enumerator, playbackOrder);
                }
            }
        }

        return this;
    }

    public async Task<ISchedulingEngine> AddPlaylist(string key, string playlist, string playlistGroup)
    {
        if (!_enumerators.ContainsKey(key))
        {
            int index = _enumerators.Count;
            List<MediaItem> items = [];
            Dictionary<PlaylistItem, List<MediaItem>> itemMap =
                await mediaCollectionRepository.GetPlaylistItemMap(playlistGroup, playlist);

            var state = new CollectionEnumeratorState { Seed = _state.Seed + index, Index = 0 };

            var enumerator = await PlaylistEnumerator.Create(
                mediaCollectionRepository,
                itemMap,
                state,
                false,
                CancellationToken.None);

            string historyKey = HistoryDetails.KeyForSchedulingContent(key, PlaybackOrder.None);
            var details = new EnumeratorDetails(enumerator, historyKey, PlaybackOrder.None);

            if (_enumerators.TryAdd(key, details))
            {
                logger.LogDebug(
                    "Added playlist {Group} / {Name} with key {Key}",
                    playlistGroup,
                    playlist,
                    key);

                ApplyHistory(historyKey, items, enumerator, PlaybackOrder.None);
            }
        }

        return this;
    }

    public async Task<ISchedulingEngine> AddSmartCollection(
        string key,
        string smartCollectionName,
        PlaybackOrder playbackOrder)
    {
        if (!_enumerators.ContainsKey(key))
        {
            int index = _enumerators.Count;
            List<MediaItem> items = await mediaCollectionRepository.GetSmartCollectionItemsByName(smartCollectionName);
            if (items.Count == 0)
            {
                logger.LogWarning("Skipping invalid or empty smart collection {Name}", smartCollectionName);
                return this;
            }

            var state = new CollectionEnumeratorState { Seed = _state.Seed + index, Index = 0 };
            foreach (var enumerator in EnumeratorForContent(items, state, playbackOrder))
            {
                string historyKey = HistoryDetails.KeyForSchedulingContent(key, playbackOrder);
                var details = new EnumeratorDetails(enumerator, historyKey, playbackOrder);

                if (_enumerators.TryAdd(key, details))
                {
                    logger.LogDebug(
                        "Added smart collection {Name} with key {Key} and order {Order}",
                        smartCollectionName,
                        key,
                        playbackOrder);

                    ApplyHistory(historyKey, items, enumerator, playbackOrder);
                }
            }
        }

        return this;
    }

    public async Task<ISchedulingEngine> AddSearch(string key, string query, PlaybackOrder playbackOrder)
    {
        if (!_enumerators.ContainsKey(key))
        {
            int index = _enumerators.Count;
            List<MediaItem> items = await mediaCollectionRepository.GetSmartCollectionItems(query, string.Empty);
            if (items.Count == 0)
            {
                logger.LogWarning("Skipping invalid or empty search query {Query}", query);
                return this;
            }

            var state = new CollectionEnumeratorState { Seed = _state.Seed + index, Index = 0 };
            foreach (var enumerator in EnumeratorForContent(items, state, playbackOrder))
            {
                string historyKey = HistoryDetails.KeyForSchedulingContent(key, playbackOrder);
                var details = new EnumeratorDetails(enumerator, historyKey, playbackOrder);

                if (_enumerators.TryAdd(key, details))
                {
                    logger.LogDebug(
                        "Added search query {Query} with key {Key} and order {Order}",
                        query,
                        key,
                        playbackOrder);

                    ApplyHistory(historyKey, items, enumerator, playbackOrder);
                }
            }
        }

        return this;
    }

    public async Task<ISchedulingEngine> AddShow(
        string key,
        Dictionary<string, string> guids,
        PlaybackOrder playbackOrder)
    {
        if (!_enumerators.ContainsKey(key))
        {
            int index = _enumerators.Count;
            List<MediaItem> items =
                await mediaCollectionRepository.GetShowItemsByShowGuids(
                    guids.Map(g => $"{g.Key}://{g.Value}").ToList());
            if (items.Count == 0)
            {
                logger.LogWarning("Skipping invalid or empty show with key {Key}", key);
                return this;
            }

            var state = new CollectionEnumeratorState { Seed = _state.Seed + index, Index = 0 };
            foreach (var enumerator in EnumeratorForContent(items, state, playbackOrder))
            {
                string historyKey = HistoryDetails.KeyForSchedulingContent(key, playbackOrder);
                var details = new EnumeratorDetails(enumerator, historyKey, playbackOrder);

                if (_enumerators.TryAdd(key, details))
                {
                    logger.LogDebug(
                        "Added show with key {Key} and order {Order}",
                        key,
                        playbackOrder);

                    ApplyHistory(historyKey, items, enumerator, playbackOrder);
                }
            }
        }

        return this;
    }

    public bool AddCount(
        string content,
        int count,
        Option<FillerKind> fillerKind,
        string customTitle,
        bool disableWatermarks)
    {
        if (!_enumerators.TryGetValue(content, out EnumeratorDetails enumeratorDetails))
        {
            logger.LogWarning("Skipping invalid content {Key}", content);
            return false;
        }

        var result = false;

        for (var i = 0; i < count; i++)
        {
            // foreach (string preRollSequence in context.GetPreRollSequence())
            // {
            //     context.PushFillerKind(FillerKind.PreRoll);
            //     await executeSequence(preRollSequence);
            //     context.PopFillerKind();
            // }

            foreach (MediaItem mediaItem in enumeratorDetails.Enumerator.Current)
            {
                TimeSpan itemDuration = DurationForMediaItem(mediaItem);

                // create a playout item
                var playoutItem = new PlayoutItem
                {
                    PlayoutId = _state.PlayoutId,
                    MediaItemId = mediaItem.Id,
                    Start = _state.CurrentTime.UtcDateTime,
                    Finish = _state.CurrentTime.UtcDateTime + itemDuration,
                    InPoint = TimeSpan.Zero,
                    OutPoint = itemDuration,
                    FillerKind = GetFillerKind(fillerKind),
                    CustomTitle = string.IsNullOrWhiteSpace(customTitle) ? null : customTitle,
                    DisableWatermarks = disableWatermarks,
                    GuideGroup = _state.PeekNextGuideGroup(),
                    PlayoutItemWatermarks = [],
                    PlayoutItemGraphicsElements = []
                };

                // foreach (int watermarkId in context.GetChannelWatermarkIds())
                // {
                //     playoutItem.PlayoutItemWatermarks.Add(
                //         new PlayoutItemWatermark
                //         {
                //             PlayoutItem = playoutItem,
                //             WatermarkId = watermarkId
                //         });
                // }
                //
                // foreach ((int graphicsElementId, string variablesJson) in context.GetGraphicsElements())
                // {
                //     playoutItem.PlayoutItemGraphicsElements.Add(
                //         new PlayoutItemGraphicsElement
                //         {
                //             PlayoutItem = playoutItem,
                //             GraphicsElementId = graphicsElementId,
                //             Variables = variablesJson
                //         });
                // }

                //await AddItemAndMidRoll(context, playoutItem, mediaItem, executeSequence);

                _state.AddedItems.Add(playoutItem);
                _state.CurrentTime += playoutItem.OutPoint - playoutItem.InPoint;
                _state.AdvanceGuideGroup();

                // create history record
                List<PlayoutHistory> maybeHistory = GetHistoryForItem(enumeratorDetails, playoutItem, mediaItem);
                foreach (PlayoutHistory history in maybeHistory)
                {
                    _state.AddedHistory.Add(history);
                }

                enumeratorDetails.Enumerator.MoveNext();

                result = true;
            }

            // foreach (string postRollSequence in context.GetPostRollSequence())
            // {
            //     context.PushFillerKind(FillerKind.PostRoll);
            //     await executeSequence(postRollSequence);
            //     context.PopFillerKind();
            // }
        }

        return result;
    }

    public ISchedulingEngine WaitUntil(TimeOnly waitUntil, bool tomorrow, bool rewindOnReset)
    {
        var currentTime = _state.CurrentTime;

        var dayOnly = DateOnly.FromDateTime(currentTime.LocalDateTime);
        var timeOnly = TimeOnly.FromDateTime(currentTime.LocalDateTime);

        if (timeOnly > waitUntil)
        {
            if (tomorrow)
            {
                // this is wrong when offset changes
                dayOnly = dayOnly.AddDays(1);
                currentTime = new DateTimeOffset(dayOnly, waitUntil, currentTime.Offset);
            }
            else if (rewindOnReset && _state.Mode == PlayoutBuildMode.Reset)
            {
                // maybe wrong when offset changes?
                currentTime = new DateTimeOffset(dayOnly, waitUntil, currentTime.Offset);
            }
        }
        else
        {
            // this is wrong when offset changes
            currentTime = new DateTimeOffset(dayOnly, waitUntil, currentTime.Offset);
        }

        _state.CurrentTime = currentTime;

        return this;
    }

    public PlayoutAnchor GetAnchor()
    {
        DateTime maxTime = _state.CurrentTime.UtcDateTime;
        if (_state.AddedItems.Count > 0)
        {
            maxTime = _state.AddedItems.Max(i => i.Finish);
        }

        return new PlayoutAnchor
        {
            NextStart = maxTime,
            Context = _state.SerializeContext()
        };
    }

    public ISchedulingEngineState GetState()
    {
        return _state;
    }

    private static Option<IMediaCollectionEnumerator> EnumeratorForContent(
        List<MediaItem> items,
        CollectionEnumeratorState state,
        PlaybackOrder playbackOrder,
        bool multiPart = false)
    {
        switch (playbackOrder)
        {
            case PlaybackOrder.Chronological:
                return new ChronologicalMediaCollectionEnumerator(items, state);
            case PlaybackOrder.Shuffle:
                bool keepMultiPartEpisodesTogether = multiPart;
                List<GroupedMediaItem> groupedMediaItems = keepMultiPartEpisodesTogether
                    ? MultiPartEpisodeGrouper.GroupMediaItems(items, false)
                    : items.Map(mi => new GroupedMediaItem(mi, null)).ToList();
                return new BlockPlayoutShuffledMediaCollectionEnumerator(groupedMediaItems, state);
        }

        return Option<IMediaCollectionEnumerator>.None;
    }

    private void ApplyHistory(
        string historyKey,
        List<MediaItem> collectionItems,
        IMediaCollectionEnumerator enumerator,
        PlaybackOrder playbackOrder)
    {
        DateTime historyTime = _state.CurrentTime.UtcDateTime;

        var filteredHistory = _referenceData.PlayoutHistory.ToList();
        filteredHistory.RemoveAll(h => _state.HistoryToRemove.Contains(h.Id));

        Option<DateTime> maxWhen = filteredHistory
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When < historyTime)
            .Map(h => h.When)
            .OrderByDescending(h => h)
            .HeadOrNone()
            .IfNone(DateTime.MinValue);

        var maybeHistory = filteredHistory
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When == maxWhen)
            .ToList();

        if (enumerator is PlaylistEnumerator playlistEnumerator)
        {
            Option<PlayoutHistory> maybePrimaryHistory = maybeHistory
                .Filter(h => string.IsNullOrWhiteSpace(h.ChildKey))
                .HeadOrNone();

            foreach (PlayoutHistory primaryHistory in maybePrimaryHistory)
            {
                var hasSetEnumeratorIndex = false;

                var childEnumeratorKeys = playlistEnumerator.ChildEnumerators.Map(x => x.CollectionKey).ToList();
                foreach ((IMediaCollectionEnumerator childEnumerator, CollectionKey collectionKey) in
                         playlistEnumerator.ChildEnumerators)
                {
                    PlaybackOrder itemPlaybackOrder = childEnumerator switch
                    {
                        ChronologicalMediaCollectionEnumerator => PlaybackOrder.Chronological,
                        RandomizedMediaCollectionEnumerator => PlaybackOrder.Random,
                        ShuffledMediaCollectionEnumerator => PlaybackOrder.Shuffle,
                        _ => PlaybackOrder.None
                    };

                    Option<PlayoutHistory> maybeApplicableHistory = maybeHistory
                        .Filter(h => h.ChildKey == HistoryDetails.KeyForCollectionKey(collectionKey))
                        .HeadOrNone();

                    if (collectionItems.Count == 0)
                    {
                        continue;
                    }

                    foreach (PlayoutHistory h in maybeApplicableHistory)
                    {
                        // logger.LogDebug(
                        //     "History is applicable: {When}: {ChildKey} / {History} / {IsCurrentChild}",
                        //     h.When,
                        //     h.ChildKey,
                        //     h.Details,
                        //     h.IsCurrentChild);

                        enumerator.ResetState(
                            new CollectionEnumeratorState
                            {
                                Seed = enumerator.State.Seed,
                                Index = h.Index + (h.IsCurrentChild ? 1 : 0)
                            });

                        if (itemPlaybackOrder is PlaybackOrder.Chronological)
                        {
                            HistoryDetails.MoveToNextItem(
                                collectionItems,
                                h.Details,
                                childEnumerator,
                                itemPlaybackOrder,
                                true);
                        }

                        if (h.IsCurrentChild)
                        {
                            // try to find enumerator based on collection key
                            playlistEnumerator.SetEnumeratorIndex(childEnumeratorKeys.IndexOf(collectionKey));
                            hasSetEnumeratorIndex = true;
                        }
                    }
                }

                if (!hasSetEnumeratorIndex)
                {
                    // falling back to enumerator based on index
                    playlistEnumerator.SetEnumeratorIndex(primaryHistory.Index);
                }

                // only move next at the end, because that may also move
                // the enumerator index
                playlistEnumerator.MoveNext();
            }
        }
        else
        {
            if (collectionItems.Count == 0)
            {
                return;
            }

            // seek to the appropriate place in the collection enumerator
            foreach (PlayoutHistory h in maybeHistory)
            {
                // logger.LogDebug("History is applicable: {When}: {History}", h.When, h.Details);

                enumerator.ResetState(
                    new CollectionEnumeratorState { Seed = enumerator.State.Seed, Index = h.Index + 1 });

                if (playbackOrder is PlaybackOrder.Chronological)
                {
                    HistoryDetails.MoveToNextItem(
                        collectionItems,
                        h.Details,
                        enumerator,
                        playbackOrder);
                }
            }
        }
    }

    private static TimeSpan DurationForMediaItem(MediaItem mediaItem)
    {
        if (mediaItem is Image image)
        {
            return TimeSpan.FromSeconds(image.ImageMetadata.Head().DurationSeconds ?? Image.DefaultSeconds);
        }

        MediaVersion version = mediaItem.GetHeadVersion();
        return version.Duration;
    }

    private List<PlayoutHistory> GetHistoryForItem(
        EnumeratorDetails enumeratorDetails,
        PlayoutItem playoutItem,
        MediaItem mediaItem)
    {
        var result = new List<PlayoutHistory>();

        if (enumeratorDetails.Enumerator is PlaylistEnumerator playlistEnumerator)
        {
            // create a playout history record
            var nextHistory = new PlayoutHistory
            {
                PlayoutId = _state.PlayoutId,
                PlaybackOrder = enumeratorDetails.PlaybackOrder,
                Index = playlistEnumerator.EnumeratorIndex,
                When = playoutItem.StartOffset.UtcDateTime,
                Finish = playoutItem.FinishOffset.UtcDateTime,
                Key = enumeratorDetails.HistoryKey,
                Details = HistoryDetails.ForMediaItem(mediaItem)
            };

            result.Add(nextHistory);

            for (var i = 0; i < playlistEnumerator.ChildEnumerators.Count; i++)
            {
                (IMediaCollectionEnumerator childEnumerator, CollectionKey collectionKey) =
                    playlistEnumerator.ChildEnumerators[i];
                bool isCurrentChild = i == playlistEnumerator.EnumeratorIndex;
                foreach (MediaItem currentMediaItem in childEnumerator.Current)
                {
                    // create a playout history record
                    var childHistory = new PlayoutHistory
                    {
                        PlayoutId = _state.PlayoutId,
                        PlaybackOrder = enumeratorDetails.PlaybackOrder,
                        Index = childEnumerator.State.Index,
                        When = playoutItem.StartOffset.UtcDateTime,
                        Finish = playoutItem.FinishOffset.UtcDateTime,
                        Key = enumeratorDetails.HistoryKey,
                        ChildKey = HistoryDetails.KeyForCollectionKey(collectionKey),
                        IsCurrentChild = isCurrentChild,
                        Details = HistoryDetails.ForMediaItem(currentMediaItem)
                    };

                    result.Add(childHistory);
                }
            }
        }
        else
        {
            // create a playout history record
            var nextHistory = new PlayoutHistory
            {
                PlayoutId = _state.PlayoutId,
                PlaybackOrder = enumeratorDetails.PlaybackOrder,
                Index = enumeratorDetails.Enumerator.State.Index,
                When = playoutItem.StartOffset.UtcDateTime,
                Finish = playoutItem.FinishOffset.UtcDateTime,
                Key = enumeratorDetails.HistoryKey,
                Details = HistoryDetails.ForMediaItem(mediaItem)
            };

            result.Add(nextHistory);
        }

        return result;
    }

    protected static FillerKind GetFillerKind(Option<FillerKind> maybeFillerKind)
    {
        foreach (FillerKind fillerKind in maybeFillerKind)
        {
            return fillerKind;
        }

        // foreach (FillerKind fillerKind in _state.GetFillerKind())
        // {
        //     return fillerKind;
        // }

        return FillerKind.None;
    }

    public record SerializedState(
        int? GuideGroup,
        bool? GuideGroupLocked);

    private class SchedulingEngineState(int guideGroup) : ISchedulingEngineState
    {
        private int _guideGroup = guideGroup;
        private bool _guideGroupLocked;

        // state
        public int PlayoutId { get; set; }
        public PlayoutBuildMode Mode { get; set; }
        public int Seed { get; set; }
        public DateTimeOffset Finish { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset CurrentTime { get; set; }

        // guide group
        public int PeekNextGuideGroup()
        {
            if (_guideGroupLocked)
            {
                return _guideGroup;
            }

            int result = _guideGroup + 1;
            if (result > 1000)
            {
                result = 1;
            }

            return result;
        }

        public void AdvanceGuideGroup()
        {
            if (_guideGroupLocked)
            {
                return;
            }

            _guideGroup++;
            if (_guideGroup > 1000)
            {
                _guideGroup = 1;
            }
        }

        public void LockGuideGroup(bool advance = true)
        {
            if (advance)
            {
                AdvanceGuideGroup();
            }

            _guideGroupLocked = true;
        }

        public void UnlockGuideGroup() => _guideGroupLocked = false;

        // result
        public Option<DateTimeOffset> RemoveBefore { get; set; }
        public bool ClearItems { get; set; }
        public List<PlayoutItem> AddedItems { get; } = [];
        public System.Collections.Generic.HashSet<int> HistoryToRemove { get; } = [];
        public List<PlayoutHistory> AddedHistory { get; } = [];

        public string SerializeContext()
        {
            // string preRollSequence = null;
            // foreach (string sequence in _preRollSequence)
            // {
            //     preRollSequence = sequence;
            // }

            var state = new SerializedState(
                _guideGroup,
                _guideGroupLocked);

            return JsonConvert.SerializeObject(state, Formatting.None, JsonSettings);
        }

        public void LoadContext(SerializedState state)
        {
            foreach (int guideGroup in Optional(state.GuideGroup))
            {
                _guideGroup = guideGroup;
            }

            foreach (bool guideGroupLocked in Optional(state.GuideGroupLocked))
            {
                _guideGroupLocked = guideGroupLocked;
            }
        }
    }

    private record EnumeratorDetails(
        IMediaCollectionEnumerator Enumerator,
        string HistoryKey,
        PlaybackOrder PlaybackOrder);
}
