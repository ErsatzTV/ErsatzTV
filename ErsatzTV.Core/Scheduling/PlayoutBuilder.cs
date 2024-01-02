using System.Reflection;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Map = LanguageExt.Map;

namespace ErsatzTV.Core.Scheduling;

// TODO: these tests fail on days when offset changes
// because the change happens during the playout
public class PlayoutBuilder : IPlayoutBuilder
{
    private static readonly Random Random = new();
    private readonly IArtistRepository _artistRepository;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<PlayoutBuilder> _logger;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;
    private readonly IMultiEpisodeShuffleCollectionEnumeratorFactory _multiEpisodeFactory;
    private readonly ITelevisionRepository _televisionRepository;

    public PlayoutBuilder(
        IConfigElementRepository configElementRepository,
        IMediaCollectionRepository mediaCollectionRepository,
        ITelevisionRepository televisionRepository,
        IArtistRepository artistRepository,
        IMultiEpisodeShuffleCollectionEnumeratorFactory multiEpisodeFactory,
        ILocalFileSystem localFileSystem,
        ILogger<PlayoutBuilder> logger)
    {
        _configElementRepository = configElementRepository;
        _mediaCollectionRepository = mediaCollectionRepository;
        _televisionRepository = televisionRepository;
        _artistRepository = artistRepository;
        _multiEpisodeFactory = multiEpisodeFactory;
        _localFileSystem = localFileSystem;
        _logger = logger;
    }

    public async Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken)
    {
        foreach (PlayoutParameters parameters in await Validate(playout))
        {
            // for testing purposes
            // if (mode == PlayoutBuildMode.Reset)
            // {
            //     return await Build(playout, mode, parameters with { Start = parameters.Start.AddDays(-2) });
            // }

            return await Build(playout, mode, parameters, cancellationToken);
        }

        return playout;
    }

    private Task<Playout> Build(
        Playout playout,
        PlayoutBuildMode mode,
        PlayoutParameters parameters,
        CancellationToken cancellationToken) =>
        mode switch
        {
            PlayoutBuildMode.Refresh => RefreshPlayout(playout, parameters, cancellationToken),
            PlayoutBuildMode.Reset => ResetPlayout(playout, parameters, cancellationToken),
            _ => ContinuePlayout(playout, parameters, cancellationToken)
        };

    internal async Task<Playout> Build(
        Playout playout,
        PlayoutBuildMode mode,
        DateTimeOffset start,
        DateTimeOffset finish,
        CancellationToken cancellationToken)
    {
        foreach (PlayoutParameters parameters in await Validate(playout))
        {
            return await Build(playout, mode, parameters with { Start = start, Finish = finish }, cancellationToken);
        }

        return playout;
    }

    private async Task<Playout> RefreshPlayout(
        Playout playout,
        PlayoutParameters parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Refreshing playout {PlayoutId} for channel {ChannelNumber} - {ChannelName}",
            playout.Id,
            playout.Channel.Number,
            playout.Channel.Name);

        playout.Items.Clear();
        playout.Anchor = null;

        // foreach (PlayoutProgramScheduleAnchor anchor in playout.ProgramScheduleAnchors)
        // {
        //     anchor.Playout = null;
        //     anchor.Collection = null;
        //     anchor.ProgramSchedule = null;
        // }

        // _logger.LogDebug("All anchors: {@Anchors}", playout.ProgramScheduleAnchors);

        // remove null anchor date ("continue" anchors)
        playout.ProgramScheduleAnchors.RemoveAll(a => a.AnchorDate is null);

        // _logger.LogDebug("Checkpoint anchors: {@Anchors}", playout.ProgramScheduleAnchors);

        // remove old checkpoints
        playout.ProgramScheduleAnchors.RemoveAll(
            a => a.AnchorDateOffset.IfNone(SystemTime.MaxValueUtc) < parameters.Start.Date);

        // remove new checkpoints
        playout.ProgramScheduleAnchors.RemoveAll(
            a => a.AnchorDateOffset.IfNone(SystemTime.MinValueUtc).Date > parameters.Start.Date);

        // _logger.LogDebug("Remaining anchors: {@Anchors}", playout.ProgramScheduleAnchors);

        var allAnchors = playout.ProgramScheduleAnchors.ToList();

        var collectionIds = playout.ProgramScheduleAnchors.Map(a => Optional(a.CollectionId)).Somes().ToHashSet();

        var multiCollectionIds =
            playout.ProgramScheduleAnchors.Map(a => Optional(a.MultiCollectionId)).Somes().ToHashSet();

        var smartCollectionIds =
            playout.ProgramScheduleAnchors.Map(a => Optional(a.SmartCollectionId)).Somes().ToHashSet();

        var mediaItemIds = playout.ProgramScheduleAnchors.Map(a => Optional(a.MediaItemId)).Somes().ToHashSet();

        playout.ProgramScheduleAnchors.Clear();

        foreach (int collectionId in collectionIds)
        {
            PlayoutProgramScheduleAnchor minAnchor = allAnchors.Filter(a => a.CollectionId == collectionId)
                .MinBy(a => a.AnchorDateOffset.IfNone(DateTimeOffset.MaxValue).Ticks);
            playout.ProgramScheduleAnchors.Add(minAnchor);
        }

        foreach (int multiCollectionId in multiCollectionIds)
        {
            PlayoutProgramScheduleAnchor minAnchor = allAnchors.Filter(a => a.MultiCollectionId == multiCollectionId)
                .MinBy(a => a.AnchorDateOffset.IfNone(DateTimeOffset.MaxValue).Ticks);
            playout.ProgramScheduleAnchors.Add(minAnchor);
        }

        foreach (int smartCollectionId in smartCollectionIds)
        {
            PlayoutProgramScheduleAnchor minAnchor = allAnchors.Filter(a => a.SmartCollectionId == smartCollectionId)
                .MinBy(a => a.AnchorDateOffset.IfNone(DateTimeOffset.MaxValue).Ticks);
            playout.ProgramScheduleAnchors.Add(minAnchor);
        }

        foreach (int mediaItemId in mediaItemIds)
        {
            PlayoutProgramScheduleAnchor minAnchor = allAnchors.Filter(a => a.MediaItemId == mediaItemId)
                .MinBy(a => a.AnchorDateOffset.IfNone(DateTimeOffset.MaxValue).Ticks);
            playout.ProgramScheduleAnchors.Add(minAnchor);
        }

        // _logger.LogDebug("Oldest anchors for each collection: {@Anchors}", playout.ProgramScheduleAnchors);

        // convert checkpoints to non-checkpoints
        // foreach (PlayoutProgramScheduleAnchor anchor in playout.ProgramScheduleAnchors)
        // {
        //     anchor.AnchorDate = null;
        // }

        // _logger.LogDebug("Final anchors: {@Anchors}", playout.ProgramScheduleAnchors);

        Option<DateTime> maybeAnchorDate = playout.ProgramScheduleAnchors
            .Map(a => Optional(a.AnchorDate))
            .Somes()
            .HeadOrNone();

        foreach (DateTime anchorDate in maybeAnchorDate)
        {
            playout.Anchor = new PlayoutAnchor
            {
                NextStart = anchorDate
            };
        }

        return await BuildPlayoutItems(
            playout,
            parameters.Start,
            parameters.Finish,
            parameters.CollectionMediaItems,
            false,
            cancellationToken);
    }

    private async Task<Playout> ResetPlayout(
        Playout playout,
        PlayoutParameters parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Resetting playout {PlayoutId} for channel {ChannelNumber} - {ChannelName}",
            playout.Id,
            playout.Channel.Number,
            playout.Channel.Name);

        playout.Items.Clear();
        playout.Anchor = null;
        playout.ProgramScheduleAnchors.Clear();

        await BuildPlayoutItems(
            playout,
            parameters.Start,
            parameters.Finish,
            parameters.CollectionMediaItems,
            true,
            cancellationToken);

        return playout;
    }

    private async Task<Playout> ContinuePlayout(
        Playout playout,
        PlayoutParameters parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Building playout {PlayoutId} for channel {ChannelNumber} - {ChannelName}",
            playout.Id,
            playout.Channel.Number,
            playout.Channel.Name);

        // remove old checkpoints
        playout.ProgramScheduleAnchors.RemoveAll(
            a => a.AnchorDateOffset.IfNone(SystemTime.MaxValueUtc) < parameters.Start.Date);

        // _logger.LogDebug("Remaining anchors: {@Anchors}", playout.ProgramScheduleAnchors);

        await BuildPlayoutItems(
            playout,
            parameters.Start,
            parameters.Finish,
            parameters.CollectionMediaItems,
            false,
            cancellationToken);

        return playout;
    }

    private async Task<Option<PlayoutParameters>> Validate(Playout playout)
    {
        Map<CollectionKey, List<MediaItem>> collectionMediaItems = await GetCollectionMediaItems(playout);
        if (collectionMediaItems.IsEmpty)
        {
            _logger.LogWarning("Playout {Playout} has no items", playout.Channel.Name);
            return None;
        }

        Option<bool> skipMissingItems =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.PlayoutSkipMissingItems);

        Option<CollectionKey> maybeEmptyCollection = await CheckForEmptyCollections(
            collectionMediaItems,
            await skipMissingItems.IfNoneAsync(false));

        foreach (CollectionKey emptyCollection in maybeEmptyCollection)
        {
            Option<string> maybeName = await _mediaCollectionRepository.GetNameFromKey(emptyCollection);
            if (maybeName.IsSome)
            {
                foreach (string name in maybeName)
                {
                    _logger.LogError(
                        "Unable to rebuild playout; {CollectionType} {CollectionName} has no valid items!",
                        emptyCollection.CollectionType,
                        name);
                }
            }
            else
            {
                _logger.LogError(
                    "Unable to rebuild playout; collection {@CollectionKey} has no valid items!",
                    emptyCollection);
            }

            return None;
        }

        playout.Items ??= new List<PlayoutItem>();
        playout.ProgramScheduleAnchors ??= new List<PlayoutProgramScheduleAnchor>();

        Option<int> daysToBuild = await _configElementRepository.GetValue<int>(ConfigElementKey.PlayoutDaysToBuild);

        DateTimeOffset now = DateTimeOffset.Now;

        return new PlayoutParameters(
            now,
            now.AddDays(await daysToBuild.IfNoneAsync(2)),
            collectionMediaItems);
    }

    private async Task<Playout> BuildPlayoutItems(
        Playout playout,
        DateTimeOffset playoutStart,
        DateTimeOffset playoutFinish,
        Map<CollectionKey, List<MediaItem>> collectionMediaItems,
        bool randomStartPoint,
        CancellationToken cancellationToken)
    {
        DateTimeOffset trimBefore = playoutStart.AddHours(-4);
        DateTimeOffset trimAfter = playoutFinish;

        DateTimeOffset start = playoutStart;
        DateTimeOffset finish = playoutStart.Date.AddDays(1);

        // _logger.LogDebug(
        //     "Trim before: {TrimBefore}, Start: {Start}, Finish: {Finish}, PlayoutFinish: {PlayoutFinish}",
        //     trimBefore,
        //     start,
        //     finish,
        //     playoutFinish);

        // build each day with "continue" anchors
        while (finish < playoutFinish)
        {
            _logger.LogDebug("Building playout from {Start} to {Finish}", start, finish);
            playout = await BuildPlayoutItems(
                playout,
                start,
                finish,
                collectionMediaItems,
                true,
                randomStartPoint,
                cancellationToken);

            // only randomize once (at the start of the playout)
            randomStartPoint = false;

            start = playout.Anchor.NextStartOffset;
            finish = finish.AddDays(1);
        }

        if (start < playoutFinish)
        {
            // build one final time without continue anchors
            _logger.LogDebug("Building final playout from {Start} to {Finish}", start, playoutFinish);
            playout = await BuildPlayoutItems(
                playout,
                start,
                playoutFinish,
                collectionMediaItems,
                false,
                randomStartPoint,
                cancellationToken);
        }

        // remove old items
        playout.Items.RemoveAll(old => old.FinishOffset < trimBefore);

        // check for future items that aren't grouped inside range
        var futureItems = playout.Items.Filter(i => i.StartOffset > trimAfter).ToList();
        foreach (PlayoutItem futureItem in futureItems)
        {
            if (playout.Items.All(i => i == futureItem || i.GuideGroup != futureItem.GuideGroup))
            {
                _logger.LogError(
                    "Playout item scheduled for {Time} after hard stop of {HardStop}",
                    futureItem.StartOffset,
                    trimAfter);

                // it feels hacky to have to clean up a playlist like this,
                // so only log the error, and leave the bad data to fail tests
                // playout.Items.Remove(futureItem);
            }
        }

        return playout;
    }

    private async Task<Playout> BuildPlayoutItems(
        Playout playout,
        DateTimeOffset playoutStart,
        DateTimeOffset playoutFinish,
        Map<CollectionKey, List<MediaItem>> collectionMediaItems,
        bool saveAnchorDate,
        bool randomStartPoint,
        CancellationToken cancellationToken)
    {
        ProgramSchedule activeSchedule = PlayoutScheduleSelector.GetProgramScheduleFor(
            playout.ProgramSchedule,
            playout.ProgramScheduleAlternates,
            playoutStart);

        // random start points are disabled in some scenarios, so ensure it's enabled and active
        randomStartPoint = randomStartPoint && activeSchedule.RandomStartPoint;

        var sortedScheduleItems = activeSchedule.Items.OrderBy(i => i.Index).ToList();
        CollectionEnumeratorState scheduleItemsEnumeratorState =
            playout.Anchor?.ScheduleItemsEnumeratorState ?? new CollectionEnumeratorState
                { Seed = Random.Next(), Index = 0 };
        IScheduleItemsEnumerator scheduleItemsEnumerator = activeSchedule.ShuffleScheduleItems
            ? new ShuffledScheduleItemsEnumerator(activeSchedule.Items, scheduleItemsEnumeratorState)
            : new OrderedScheduleItemsEnumerator(activeSchedule.Items, scheduleItemsEnumeratorState);

        var collectionEnumerators = new Dictionary<CollectionKey, IMediaCollectionEnumerator>();
        foreach ((CollectionKey collectionKey, List<MediaItem> mediaItems) in collectionMediaItems)
        {
            // use configured playback order for primary collection, shuffle for filler
            Option<ProgramScheduleItem> maybeScheduleItem = sortedScheduleItems
                .FirstOrDefault(item => CollectionKey.ForScheduleItem(item) == collectionKey);
            PlaybackOrder playbackOrder = maybeScheduleItem
                .Match(item => item.PlaybackOrder, () => PlaybackOrder.Shuffle);
            IMediaCollectionEnumerator enumerator =
                await GetMediaCollectionEnumerator(
                    playout,
                    activeSchedule,
                    collectionKey,
                    mediaItems,
                    playbackOrder,
                    randomStartPoint,
                    cancellationToken);
            collectionEnumerators.Add(collectionKey, enumerator);
        }
        
        var collectionItemCount = collectionMediaItems.Map((k, v) => (k, v.Count)).Values.ToDictionary();
        
        var scheduleItemsFillGroupEnumerators = new Dictionary<int, IScheduleItemsEnumerator>();
        foreach (ProgramScheduleItem scheduleItem in sortedScheduleItems.Where(si => si.FillWithGroupMode is not FillWithGroupMode.None))
        {
            var collectionKey = CollectionKey.ForScheduleItem(scheduleItem);
            List<MediaItem> mediaItems = await MediaItemsForCollection.Collect(
                _mediaCollectionRepository,
                _televisionRepository,
                _artistRepository,
                collectionKey);
            var fakeCollections = _mediaCollectionRepository.GroupIntoFakeCollections(mediaItems)
                .Filter(c => c.ShowId > 0 || c.ArtistId > 0)
                .ToList();
            List<ProgramScheduleItem> fakeScheduleItems = [];
            
            // this will be used to clone a schedule item 
            MethodInfo generic = typeof(JsonConvert).GetMethods()
                .FirstOrDefault(
                    x => x.Name.Equals("DeserializeObject", StringComparison.OrdinalIgnoreCase) && x.IsGenericMethod &&
                         x.GetParameters().Length == 1)?.MakeGenericMethod(scheduleItem.GetType());
            
            foreach (CollectionWithItems fakeCollection in fakeCollections)
            {
                var key = new CollectionKey
                {
                    CollectionType = fakeCollection.ShowId > 0
                        ? ProgramScheduleItemCollectionType.TelevisionShow
                        : ProgramScheduleItemCollectionType.Artist,
                    MediaItemId = fakeCollection.ShowId > 0 ? fakeCollection.ShowId : fakeCollection.ArtistId
                };

                string serialized = JsonConvert.SerializeObject(scheduleItem);
                var copyScheduleItem = generic.Invoke(this, [serialized]) as ProgramScheduleItem;
                copyScheduleItem.CollectionType = key.CollectionType;
                copyScheduleItem.MediaItemId = key.MediaItemId;
                fakeScheduleItems.Add(copyScheduleItem);

                IMediaCollectionEnumerator enumerator = await GetMediaCollectionEnumerator(
                    playout,
                    activeSchedule,
                    key,
                    fakeCollection.MediaItems,
                    scheduleItem.PlaybackOrder,
                    randomStartPoint,
                    cancellationToken);

                collectionEnumerators.Add(key, enumerator);
                
                // this makes multiple (0) work - since it needs the number of items in the collection
                collectionItemCount.Add(key, fakeCollection.MediaItems.Count);
            }

            CollectionEnumeratorState enumeratorState =
                playout.FillGroupIndices.Any(fgi => fgi.ProgramScheduleItemId == scheduleItem.Id)
                    ? playout.FillGroupIndices.Find(fgi => fgi.ProgramScheduleItemId == scheduleItem.Id).EnumeratorState
                    : new CollectionEnumeratorState { Seed = Random.Next(), Index = 0 };

            switch (scheduleItem.FillWithGroupMode)
            {
                case FillWithGroupMode.FillWithOrderedGroups:
                {
                    var enumerator = new OrderedScheduleItemsEnumerator(fakeScheduleItems, enumeratorState);
                    scheduleItemsFillGroupEnumerators[scheduleItem.Id] = enumerator;
                    break;
                }
                case FillWithGroupMode.FillWithShuffledGroups:
                {
                    var enumerator = new ShuffledScheduleItemsEnumerator(fakeScheduleItems, enumeratorState);
                    scheduleItemsFillGroupEnumerators[scheduleItem.Id] = enumerator;
                    break;
                }
            }
        }

        // find start anchor
        PlayoutAnchor startAnchor = FindStartAnchor(playout, playoutStart, scheduleItemsEnumerator);
        // _logger.LogDebug("Start anchor: {@StartAnchor}", startAnchor);

        // start at the previously-decided time
        DateTimeOffset currentTime = startAnchor.NextStartOffset.ToLocalTime();

        if (currentTime >= playoutFinish)
        {
            // nothing to do, no need to add more anchors
            return playout;
        }

        // _logger.LogDebug(
        //     "Starting playout ({PlayoutId}) for channel {ChannelNumber} - {ChannelName} at {StartTime}",
        //     playout.Id,
        //     playout.Channel.Number,
        //     playout.Channel.Name,
        //     currentTime);

        // removing any items scheduled past the start anchor
        // this could happen if the app was closed after scheduling items
        // but before saving the anchor
        int removed = playout.Items.RemoveAll(pi => pi.StartOffset >= currentTime);
        if (removed > 0)
        {
            _logger.LogWarning("Removed {Count} schedule items beyond current start anchor", removed);
        }

        // start with the previously-decided schedule item
        // start with the previous multiple/duration states
        var playoutBuilderState = new PlayoutBuilderState(
            scheduleItemsEnumerator,
            Optional(startAnchor.MultipleRemaining),
            startAnchor.DurationFinishOffset,
            startAnchor.InFlood,
            startAnchor.InDurationFiller,
            startAnchor.NextGuideGroup,
            currentTime);

        var schedulerOne = new PlayoutModeSchedulerOne(_logger);
        var schedulerMultiple = new PlayoutModeSchedulerMultiple(collectionItemCount.ToMap(), _logger);
        var schedulerDuration = new PlayoutModeSchedulerDuration(_logger);
        var schedulerFlood = new PlayoutModeSchedulerFlood(_logger);

        var timeCount = new Dictionary<DateTimeOffset, int>();

        // loop until we're done filling the desired amount of time
        while (playoutBuilderState.CurrentTime < playoutFinish && !cancellationToken.IsCancellationRequested)
        {
            if (timeCount.TryGetValue(playoutBuilderState.CurrentTime, out int count))
            {
                timeCount[playoutBuilderState.CurrentTime] = count + 1;
            }
            else
            {
                timeCount[playoutBuilderState.CurrentTime] = 1;
            }

            if (timeCount[playoutBuilderState.CurrentTime] == 6)
            {
                _logger.LogWarning(
                    "Failed to schedule beyond {Time}; aborting playout build - this is a bug",
                    playoutBuilderState.CurrentTime);

                throw new InvalidOperationException("Scheduling loop encountered");
            }

            // _logger.LogDebug("Playout time is {CurrentTime}", playoutBuilderState.CurrentTime);

            // get the schedule item out of the sorted list
            ProgramScheduleItem scheduleItem = playoutBuilderState.ScheduleItemsEnumerator.Current;
            
            // replace with the fake schedule item when filling with group
            if (scheduleItem.FillWithGroupMode is not FillWithGroupMode.None)
            {
                scheduleItem = scheduleItemsFillGroupEnumerators[scheduleItem.Id].Current;
            }

            ProgramScheduleItem nextScheduleItem = playoutBuilderState.ScheduleItemsEnumerator.Peek(1);

            Tuple<PlayoutBuilderState, List<PlayoutItem>> result = scheduleItem switch
            {
                ProgramScheduleItemMultiple multiple => schedulerMultiple.Schedule(
                    playoutBuilderState,
                    collectionEnumerators,
                    multiple,
                    nextScheduleItem,
                    playoutFinish,
                    cancellationToken),
                ProgramScheduleItemDuration duration => schedulerDuration.Schedule(
                    playoutBuilderState,
                    collectionEnumerators,
                    duration,
                    nextScheduleItem,
                    playoutFinish,
                    cancellationToken),
                ProgramScheduleItemFlood flood => schedulerFlood.Schedule(
                    playoutBuilderState,
                    collectionEnumerators,
                    flood,
                    nextScheduleItem,
                    playoutFinish,
                    cancellationToken),
                ProgramScheduleItemOne one => schedulerOne.Schedule(
                    playoutBuilderState,
                    collectionEnumerators,
                    one,
                    nextScheduleItem,
                    playoutFinish,
                    cancellationToken),
                _ => throw new NotSupportedException(nameof(scheduleItem))
            };

            (PlayoutBuilderState nextState, List<PlayoutItem> playoutItems) = result;

            // if we completed a multiple/duration block, move to the next fill group
            if (scheduleItem.FillWithGroupMode is not FillWithGroupMode.None)
            {
                if (nextState.MultipleRemaining.IsNone && nextState.DurationFinish.IsNone)
                {
                    scheduleItemsFillGroupEnumerators[scheduleItem.Id].MoveNext();
                }
            }

            foreach (PlayoutItem playoutItem in playoutItems)
            {
                playout.Items.Add(playoutItem);
            }

            playoutBuilderState = nextState;
        }

        // once more to get playout anchor
        ProgramScheduleItem anchorScheduleItem = playoutBuilderState.ScheduleItemsEnumerator.Current;

        if (playout.Items.Count != 0)
        {
            DateTimeOffset maxStartTime = playout.Items.Max(i => i.FinishOffset);
            if (maxStartTime < playoutBuilderState.CurrentTime)
            {
                playoutBuilderState = playoutBuilderState with { CurrentTime = maxStartTime };
            }
        }

        playout.Anchor = new PlayoutAnchor
        {
            ScheduleItemsEnumeratorState = playoutBuilderState.ScheduleItemsEnumerator.State,
            NextStart = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .GetStartTimeAfter(playoutBuilderState, anchorScheduleItem)
                .UtcDateTime,
            InFlood = playoutBuilderState.InFlood,
            InDurationFiller = playoutBuilderState.InDurationFiller,
            NextGuideGroup = playoutBuilderState.NextGuideGroup
        };

        foreach (int multipleRemaining in playoutBuilderState.MultipleRemaining)
        {
            playout.Anchor.MultipleRemaining = multipleRemaining;
        }

        foreach (DateTimeOffset durationFinish in playoutBuilderState.DurationFinish)
        {
            playout.Anchor.DurationFinish = durationFinish.UtcDateTime;
        }

        // build program schedule anchors
        playout.ProgramScheduleAnchors = BuildProgramScheduleAnchors(
            playout,
            activeSchedule,
            collectionEnumerators,
            saveAnchorDate);
        
        // build fill group indices
        playout.FillGroupIndices = BuildFillGroupIndices(playout, scheduleItemsFillGroupEnumerators);

        return playout;
    }

    private static List<PlayoutScheduleItemFillGroupIndex> BuildFillGroupIndices(
        Playout playout,
        Dictionary<int, IScheduleItemsEnumerator> scheduleItemsFillGroupEnumerators)
    {
        var result = playout.FillGroupIndices.ToList();
        
        foreach ((int programScheduleItemId, IScheduleItemsEnumerator enumerator) in scheduleItemsFillGroupEnumerators)
        {
            Option<PlayoutScheduleItemFillGroupIndex> maybeFgi = Optional(
                result.FirstOrDefault(fgi => fgi.ProgramScheduleItemId == programScheduleItemId));

            foreach (PlayoutScheduleItemFillGroupIndex fgi in maybeFgi)
            {
                fgi.EnumeratorState = enumerator.State;
            }
            
            if (maybeFgi.IsNone)
            {
                var fgi = new PlayoutScheduleItemFillGroupIndex
                {
                    PlayoutId = playout.Id,
                    ProgramScheduleItemId = programScheduleItemId,
                    EnumeratorState = enumerator.State
                };
                
                result.Add(fgi);
            }
        }

        return result;
    }

    private async Task<Map<CollectionKey, List<MediaItem>>> GetCollectionMediaItems(Playout playout)
    {
        var collectionKeys = playout.ProgramSchedule.Items
            .Append(playout.ProgramScheduleAlternates.Bind(psa => psa.ProgramSchedule.Items))
            .DistinctBy(i => i.Id)
            .SelectMany(CollectionKeysForItem)
            .Distinct()
            .ToList();

        IEnumerable<Tuple<CollectionKey, List<MediaItem>>> tuples = await collectionKeys.Map(
            async collectionKey => Tuple(
                collectionKey,
                await MediaItemsForCollection.Collect(
                    _mediaCollectionRepository,
                    _televisionRepository,
                    _artistRepository,
                    collectionKey))).SequenceParallel();

        return Map.createRange(tuples);
    }

    private async Task<Option<CollectionKey>> CheckForEmptyCollections(
        Map<CollectionKey, List<MediaItem>> collectionMediaItems,
        bool skipMissingItems)
    {
        foreach ((CollectionKey _, List<MediaItem> items) in collectionMediaItems)
        {
            var zeroItems = new List<MediaItem>();
            var missingItems = new List<MediaItem>();

            foreach (MediaItem item in items)
            {
                bool isZero = item switch
                {
                    Movie m => await m.MediaVersions.Map(v => v.Duration).HeadOrNone()
                        .IfNoneAsync(TimeSpan.Zero) == TimeSpan.Zero,
                    Episode e => await e.MediaVersions.Map(v => v.Duration).HeadOrNone()
                        .IfNoneAsync(TimeSpan.Zero) == TimeSpan.Zero,
                    MusicVideo mv => await mv.MediaVersions.Map(v => v.Duration).HeadOrNone()
                        .IfNoneAsync(TimeSpan.Zero) == TimeSpan.Zero,
                    OtherVideo ov => await ov.MediaVersions.Map(v => v.Duration).HeadOrNone()
                        .IfNoneAsync(TimeSpan.Zero) == TimeSpan.Zero,
                    Song s => await s.MediaVersions.Map(v => v.Duration).HeadOrNone()
                        .IfNoneAsync(TimeSpan.Zero) == TimeSpan.Zero,
                    _ => true
                };

                if (skipMissingItems && item.State is MediaItemState.FileNotFound or MediaItemState.Unavailable)
                {
                    _logger.LogWarning(
                        "Skipping media item that does not exist on disk  {MediaItem} - {MediaItemTitle} - {Path}",
                        item.Id,
                        DisplayTitle(item),
                        item.GetHeadVersion().MediaFiles.Head().Path);

                    missingItems.Add(item);
                }
                else if (isZero)
                {
                    _logger.LogWarning(
                        "Skipping media item with zero duration {MediaItem} - {MediaItemTitle}",
                        item.Id,
                        DisplayTitle(item));

                    zeroItems.Add(item);
                }
            }

            items.RemoveAll(missingItems.Contains);
            items.RemoveAll(zeroItems.Contains);
        }

        return collectionMediaItems.Find(c => c.Value.Count == 0).Map(c => c.Key);
    }

    private static PlayoutAnchor FindStartAnchor(
        Playout playout,
        DateTimeOffset start,
        IScheduleItemsEnumerator enumerator) =>
        Optional(playout.Anchor).IfNone(
            () =>
            {
                ProgramScheduleItem schedule = enumerator.Current;
                switch (schedule.StartType)
                {
                    case StartType.Fixed:
                        return new PlayoutAnchor
                        {
                            ScheduleItemsEnumeratorState = enumerator.State,
                            NextStart = (start - start.TimeOfDay).UtcDateTime +
                                        schedule.StartTime.GetValueOrDefault()
                        };
                    case StartType.Dynamic:
                    default:
                        return new PlayoutAnchor
                        {
                            ScheduleItemsEnumeratorState = enumerator.State,
                            NextStart = (start - start.TimeOfDay).UtcDateTime
                        };
                }
            });

    private static List<PlayoutProgramScheduleAnchor> BuildProgramScheduleAnchors(
        Playout playout,
        ProgramSchedule activeSchedule,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
        bool saveAnchorDate)
    {
        var result = new List<PlayoutProgramScheduleAnchor>();

        foreach (CollectionKey collectionKey in collectionEnumerators.Keys)
        {
            Option<PlayoutProgramScheduleAnchor> maybeExisting = playout.ProgramScheduleAnchors.FirstOrDefault(
                a => a.CollectionType == collectionKey.CollectionType
                     && a.CollectionId == collectionKey.CollectionId
                     && a.MediaItemId == collectionKey.MediaItemId
                     && a.SmartCollectionId == collectionKey.SmartCollectionId
                     && a.MultiCollectionId == collectionKey.MultiCollectionId
                     && a.AnchorDate is null);

            var maybeEnumeratorState = collectionEnumerators.ToDictionary(e => e.Key, e => e.Value.State);

            PlayoutProgramScheduleAnchor scheduleAnchor = maybeExisting.Match(
                existing =>
                {
                    existing.EnumeratorState = maybeEnumeratorState[collectionKey];
                    return existing;
                },
                () => new PlayoutProgramScheduleAnchor
                {
                    Playout = playout,
                    PlayoutId = playout.Id,
                    CollectionType = collectionKey.CollectionType,
                    CollectionId = collectionKey.CollectionId,
                    MultiCollectionId = collectionKey.MultiCollectionId,
                    SmartCollectionId = collectionKey.SmartCollectionId,
                    MediaItemId = collectionKey.MediaItemId,
                    EnumeratorState = maybeEnumeratorState[collectionKey]
                });

            if (saveAnchorDate)
            {
                scheduleAnchor.AnchorDate = playout.Anchor?.NextStart;
            }

            result.Add(scheduleAnchor);
        }

        foreach (PlayoutProgramScheduleAnchor checkpointAnchor in playout.ProgramScheduleAnchors.Where(
                     a => a.AnchorDate is not null))
        {
            result.Add(checkpointAnchor);
        }

        return result;
    }

    private async Task<IMediaCollectionEnumerator> GetMediaCollectionEnumerator(
        Playout playout,
        ProgramSchedule activeSchedule,
        CollectionKey collectionKey,
        List<MediaItem> mediaItems,
        PlaybackOrder playbackOrder,
        bool randomStartPoint,
        CancellationToken cancellationToken)
    {
        Option<PlayoutProgramScheduleAnchor> maybeAnchor = playout.ProgramScheduleAnchors
            .OrderByDescending(a => a.AnchorDate ?? DateTime.MaxValue)
            .FirstOrDefault(
                a => a.CollectionType == collectionKey.CollectionType
                     && a.CollectionId == collectionKey.CollectionId
                     && a.MultiCollectionId == collectionKey.MultiCollectionId
                     && a.SmartCollectionId == collectionKey.SmartCollectionId
                     && a.MediaItemId == collectionKey.MediaItemId);

        CollectionEnumeratorState state = null;

        foreach (PlayoutProgramScheduleAnchor anchor in maybeAnchor)
        {
            // _logger.LogDebug("Selecting anchor {@Anchor}", anchor);

            anchor.EnumeratorState ??= new CollectionEnumeratorState { Seed = Random.Next(), Index = 0 };

            state = anchor.EnumeratorState;
        }

        state ??= new CollectionEnumeratorState { Seed = Random.Next(), Index = 0 };

        int collectionId = collectionKey.CollectionId ?? 0;

        if (collectionKey.CollectionType == ProgramScheduleItemCollectionType.Collection &&
            await _mediaCollectionRepository.IsCustomPlaybackOrder(collectionId))
        {
            Option<Collection> maybeCollectionWithItems =
                await _mediaCollectionRepository.GetCollectionWithCollectionItemsUntracked(collectionId);

            foreach (Collection collectionWithItems in maybeCollectionWithItems)
            {
                return new CustomOrderCollectionEnumerator(
                    collectionWithItems,
                    mediaItems,
                    state);
            }
        }

        // index shouldn't ever be greater than zero with randomStartPoint since anchors shouldn't exist, but
        randomStartPoint = randomStartPoint && state.Index == 0;

        switch (playbackOrder)
        {
            case PlaybackOrder.Chronological:
                if (randomStartPoint)
                {
                    state = new CollectionEnumeratorState
                    {
                        Seed = state.Seed,
                        Index = Random.Next(0, mediaItems.Count - 1)
                    };
                }

                return new ChronologicalMediaCollectionEnumerator(mediaItems, state);
            case PlaybackOrder.SeasonEpisode:
                if (randomStartPoint)
                {
                    state = new CollectionEnumeratorState
                    {
                        Seed = state.Seed,
                        Index = Random.Next(0, mediaItems.Count - 1)
                    };
                }

                return new SeasonEpisodeMediaCollectionEnumerator(mediaItems, state);
            case PlaybackOrder.Random:
                return new RandomizedMediaCollectionEnumerator(mediaItems, state);
            case PlaybackOrder.ShuffleInOrder:
                return new ShuffleInOrderCollectionEnumerator(
                    await GetCollectionItemsForShuffleInOrder(collectionKey),
                    state,
                    activeSchedule.RandomStartPoint,
                    cancellationToken);
            case PlaybackOrder.MultiEpisodeShuffle when
                collectionKey.CollectionType == ProgramScheduleItemCollectionType.TelevisionShow &&
                collectionKey.MediaItemId.HasValue:
                foreach (Show show in await _televisionRepository.GetShow(collectionKey.MediaItemId.Value))
                {
                    foreach (MetadataGuid guid in show.ShowMetadata.Map(sm => sm.Guids).Flatten())
                    {
                        string jsScriptPath = Path.ChangeExtension(
                            Path.Combine(
                                FileSystemLayout.MultiEpisodeShuffleTemplatesFolder,
                                guid.Guid.Replace("://", "_")),
                            "js");
                        _logger.LogDebug("Checking for JS Script at {Path}", jsScriptPath);
                        if (_localFileSystem.FileExists(jsScriptPath))
                        {
                            _logger.LogDebug("Found JS Script at {Path}", jsScriptPath);
                            try
                            {
                                return _multiEpisodeFactory.Create(jsScriptPath, mediaItems, state, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                    ex,
                                    "Failed to initialize multi-episode shuffle; falling back to normal shuffle");
                            }
                        }
                    }
                }

                // fall back to shuffle if show or template cannot be found
                goto case PlaybackOrder.Shuffle;

            // fall back to shuffle when television show isn't selected
            case PlaybackOrder.MultiEpisodeShuffle:
            case PlaybackOrder.Shuffle:
                return new ShuffledMediaCollectionEnumerator(
                    await GetGroupedMediaItemsForShuffle(activeSchedule, mediaItems, collectionKey),
                    state,
                    cancellationToken);
            default:
                // TODO: handle this error case differently?
                return new RandomizedMediaCollectionEnumerator(mediaItems, state);
        }
    }

    private async Task<List<GroupedMediaItem>> GetGroupedMediaItemsForShuffle(
        ProgramSchedule activeSchedule,
        List<MediaItem> mediaItems,
        CollectionKey collectionKey)
    {
        if (collectionKey.MultiCollectionId != null)
        {
            List<CollectionWithItems> collections = await _mediaCollectionRepository
                .GetMultiCollectionCollections(collectionKey.MultiCollectionId.Value);

            return MultiCollectionGrouper.GroupMediaItems(collections);
        }

        return activeSchedule.KeepMultiPartEpisodesTogether
            ? MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, activeSchedule.TreatCollectionsAsShows)
            : mediaItems.Map(mi => new GroupedMediaItem(mi, null)).ToList();
    }

    private async Task<List<CollectionWithItems>> GetCollectionItemsForShuffleInOrder(CollectionKey collectionKey)
    {
        List<CollectionWithItems> result;

        if (collectionKey.MultiCollectionId != null)
        {
            result = await _mediaCollectionRepository.GetMultiCollectionCollections(
                collectionKey.MultiCollectionId.Value);
        }
        else
        {
            result = await _mediaCollectionRepository.GetFakeMultiCollectionCollections(
                collectionKey.CollectionId,
                collectionKey.SmartCollectionId);
        }

        return result;
    }

    internal static string DisplayTitle(MediaItem mediaItem)
    {
        switch (mediaItem)
        {
            case Episode e:
                string showTitle = e.Season.Show.ShowMetadata.HeadOrNone()
                    .Map(sm => $"{sm.Title} - ").IfNone(string.Empty);
                return e.EpisodeMetadata.HeadOrNone()
                    .Map(em => $"{showTitle}s{e.Season.SeasonNumber:00}e{em.EpisodeNumber:00} - {em.Title}")
                    .IfNone("[unknown episode]");
            case Movie m:
                return m.MovieMetadata.HeadOrNone().Match(mm => mm.Title ?? string.Empty, () => "[unknown movie]");
            case MusicVideo mv:
                string artistName = mv.Artist.ArtistMetadata.HeadOrNone()
                    .Map(am => $"{am.Title} - ").IfNone(string.Empty);
                return mv.MusicVideoMetadata.HeadOrNone()
                    .Map(mvm => $"{artistName}{mvm.Title}")
                    .IfNone("[unknown music video]");
            case OtherVideo ov:
                return ov.OtherVideoMetadata.HeadOrNone().Match(
                    ovm => ovm.Title ?? string.Empty,
                    () => "[unknown video]");
            case Song s:
                return s.SongMetadata.HeadOrNone().Match(
                    sm => sm.Title ?? string.Empty,
                    () => "[unknown song]");
            default:
                return string.Empty;
        }
    }

    private static List<CollectionKey> CollectionKeysForItem(ProgramScheduleItem item)
    {
        var result = new List<CollectionKey>
        {
            CollectionKey.ForScheduleItem(item)
        };

        if (item.PreRollFiller != null)
        {
            result.Add(CollectionKey.ForFillerPreset(item.PreRollFiller));
        }

        if (item.MidRollFiller != null)
        {
            result.Add(CollectionKey.ForFillerPreset(item.MidRollFiller));
        }

        if (item.PostRollFiller != null)
        {
            result.Add(CollectionKey.ForFillerPreset(item.PostRollFiller));
        }

        if (item.TailFiller != null)
        {
            result.Add(CollectionKey.ForFillerPreset(item.TailFiller));
        }

        if (item.FallbackFiller != null)
        {
            result.Add(CollectionKey.ForFillerPreset(item.FallbackFiller));
        }

        return result;
    }
}
