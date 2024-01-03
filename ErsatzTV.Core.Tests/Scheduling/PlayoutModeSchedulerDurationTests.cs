﻿using Destructurama;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class PlayoutModeSchedulerDurationTests : SchedulerTestBase
{
    [SetUp]
    public void SetUp() => _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    private CancellationToken _cancellationToken;
    private readonly ILogger<PlayoutModeSchedulerDuration> _logger;

    public PlayoutModeSchedulerDurationTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .Destructure.UsingAttributes()
            .CreateLogger();

        ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

        _logger = loggerFactory.CreateLogger<PlayoutModeSchedulerDuration>();
    }

    [Test]
    public void Should_Fill_Exact_Duration()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));

        var scheduleItem = new ProgramScheduleItemDuration
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlayoutDuration = TimeSpan.FromHours(3),
            TailMode = TailMode.None,
            PlaybackOrder = PlaybackOrder.Chronological,
            CustomTitle = "CustomTitle"
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            new List<ProgramScheduleItem> { scheduleItem },
            new CollectionEnumeratorState());

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerDuration(_logger);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(2); // one guide group here because of custom title
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(0);

        enumerator.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(3);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);
        playoutItems[0].GuideFinish.HasValue.Should().BeFalse();
        playoutItems[0].CustomTitle.Should().Be("CustomTitle");

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddHours(1));
        playoutItems[1].GuideGroup.Should().Be(1);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);
        playoutItems[1].GuideFinish.HasValue.Should().BeFalse();
        playoutItems[1].CustomTitle.Should().Be("CustomTitle");

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.AddHours(2));
        playoutItems[2].GuideGroup.Should().Be(1);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
        playoutItems[2].GuideFinish.HasValue.Should().BeTrue();
        playoutItems[2].CustomTitle.Should().Be("CustomTitle");
    }

    [Test]
    public void Should_Fill_Exact_Duration_CustomTitle()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));

        var scheduleItem = new ProgramScheduleItemDuration
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlayoutDuration = TimeSpan.FromHours(3),
            TailMode = TailMode.None,
            PlaybackOrder = PlaybackOrder.Chronological,
            CustomTitle = "Custom Title"
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            new List<ProgramScheduleItem> { scheduleItem },
            new CollectionEnumeratorState());

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerDuration(_logger);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(2);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(0);

        enumerator.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(3);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);
        playoutItems[0].GuideFinish.HasValue.Should().BeFalse();
        playoutItems[0].CustomTitle.Should().Be("Custom Title");

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddHours(1));
        playoutItems[1].GuideGroup.Should().Be(1);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);
        playoutItems[1].GuideFinish.HasValue.Should().BeFalse();
        playoutItems[1].CustomTitle.Should().Be("Custom Title");

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.AddHours(2));
        playoutItems[2].GuideGroup.Should().Be(1);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
        playoutItems[2].GuideFinish.HasValue.Should().BeTrue();
        playoutItems[2].CustomTitle.Should().Be("Custom Title");
    }

    [Test]
    public void Should_Not_Have_Gap_Duration_Tail_Mode_None()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));

        var scheduleItem = new ProgramScheduleItemDuration
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlayoutDuration = TimeSpan.FromHours(3),
            TailMode = TailMode.None,
            PlaybackOrder = PlaybackOrder.Chronological
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            new List<ProgramScheduleItem> { scheduleItem },
            new CollectionEnumeratorState());

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerDuration(_logger);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(0);

        enumerator.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(3);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);
        playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);
        playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
        playoutItems[2].GuideFinish.HasValue.Should().BeTrue();
    }

    [Test]
    public void Should_Have_Gap_Duration_Tail_Mode_Offline_No_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));

        var scheduleItem = new ProgramScheduleItemDuration
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlayoutDuration = TimeSpan.FromHours(3),
            TailMode = TailMode.Offline,
            PlaybackOrder = PlaybackOrder.Chronological
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            new List<ProgramScheduleItem> { scheduleItem },
            new CollectionEnumeratorState());

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerDuration(_logger);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        // duration block should end after exact duration, with gap
        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(0);

        enumerator.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(3);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);
        playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);
        playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);

        // offline should not set guide finish
        playoutItems[2].GuideFinish.HasValue.Should().BeFalse();
    }

    [Test]
    public void Should_Not_Have_Gap_Duration_Tail_Mode_Offline_With_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(1));

        var scheduleItem = new ProgramScheduleItemDuration
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlayoutDuration = TimeSpan.FromHours(3),
            TailMode = TailMode.Offline,
            PlaybackOrder = PlaybackOrder.Chronological,
            FallbackFiller = new FillerPreset
            {
                FillerKind = FillerKind.Fallback,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            }
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            new List<ProgramScheduleItem> { scheduleItem },
            new CollectionEnumeratorState());

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerDuration(_logger);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.FallbackFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(0);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(4);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);
        playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);
        playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
        playoutItems[2].GuideFinish.HasValue.Should().BeTrue();

        playoutItems[3].MediaItemId.Should().Be(3);
        playoutItems[3].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.Should().Be(3);
        playoutItems[3].FillerKind.Should().Be(FillerKind.Fallback);
        playoutItems[3].GuideFinish.HasValue.Should().BeFalse();
    }

    [Test]
    public void Should_Not_Have_Gap_Duration_Tail_Mode_Filler_Exact_Duration()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));

        var scheduleItem = new ProgramScheduleItemDuration
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlayoutDuration = TimeSpan.FromHours(3),
            TailMode = TailMode.Filler,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            }
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            new List<ProgramScheduleItem> { scheduleItem },
            new CollectionEnumeratorState());

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerDuration(_logger);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(0);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(6);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);
        playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);
        playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
        playoutItems[2].GuideFinish.HasValue.Should().BeTrue();

        playoutItems[3].MediaItemId.Should().Be(3);
        playoutItems[3].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.Should().Be(3);
        playoutItems[3].FillerKind.Should().Be(FillerKind.Tail);
        playoutItems[3].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[4].MediaItemId.Should().Be(4);
        playoutItems[4].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 50, 0)));
        playoutItems[4].GuideGroup.Should().Be(3);
        playoutItems[4].FillerKind.Should().Be(FillerKind.Tail);
        playoutItems[3].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[5].MediaItemId.Should().Be(3);
        playoutItems[5].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 55, 0)));
        playoutItems[5].GuideGroup.Should().Be(3);
        playoutItems[5].FillerKind.Should().Be(FillerKind.Tail);
        playoutItems[3].GuideFinish.HasValue.Should().BeFalse();
    }

    [Test]
    public void Should_Have_Gap_Duration_Tail_Mode_Filler_No_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));

        var scheduleItem = new ProgramScheduleItemDuration
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlayoutDuration = TimeSpan.FromHours(3),
            TailMode = TailMode.Filler,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            }
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            new List<ProgramScheduleItem> { scheduleItem },
            new CollectionEnumeratorState());

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerDuration(_logger);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 57, 0)));

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(0);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(6);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);
        playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);
        playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
        playoutItems[2].GuideFinish.HasValue.Should().BeTrue();

        playoutItems[3].MediaItemId.Should().Be(3);
        playoutItems[3].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.Should().Be(3);
        playoutItems[3].FillerKind.Should().Be(FillerKind.Tail);
        playoutItems[3].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[4].MediaItemId.Should().Be(4);
        playoutItems[4].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
        playoutItems[4].GuideGroup.Should().Be(3);
        playoutItems[4].FillerKind.Should().Be(FillerKind.Tail);
        playoutItems[4].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[5].MediaItemId.Should().Be(3);
        playoutItems[5].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
        playoutItems[5].GuideGroup.Should().Be(3);
        playoutItems[5].FillerKind.Should().Be(FillerKind.Tail);
        playoutItems[5].GuideFinish.HasValue.Should().BeFalse();
    }

    [Test]
    public void Should_Not_Have_Gap_Duration_Tail_Mode_Filler_With_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));
        Collection collectionThree = TwoItemCollection(5, 6, TimeSpan.FromMinutes(1));

        var scheduleItem = new ProgramScheduleItemDuration
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlayoutDuration = TimeSpan.FromHours(3),
            TailMode = TailMode.Filler,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            FallbackFiller = new FillerPreset
            {
                FillerKind = FillerKind.Fallback,
                Collection = collectionThree,
                CollectionId = collectionThree.Id
            }
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            new List<ProgramScheduleItem> { scheduleItem },
            new CollectionEnumeratorState());

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var enumerator3 = new ChronologicalMediaCollectionEnumerator(
            collectionThree.MediaItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerDuration(_logger);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(
                scheduleItem,
                enumerator1,
                scheduleItem.TailFiller,
                enumerator2,
                scheduleItem.FallbackFiller,
                enumerator3),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(0);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(1);
        enumerator3.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(7);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);
        playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);
        playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
        playoutItems[2].GuideFinish.HasValue.Should().BeTrue();

        playoutItems[3].MediaItemId.Should().Be(3);
        playoutItems[3].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.Should().Be(3);
        playoutItems[3].FillerKind.Should().Be(FillerKind.Tail);
        playoutItems[3].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[4].MediaItemId.Should().Be(4);
        playoutItems[4].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
        playoutItems[4].GuideGroup.Should().Be(3);
        playoutItems[4].FillerKind.Should().Be(FillerKind.Tail);
        playoutItems[4].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[5].MediaItemId.Should().Be(3);
        playoutItems[5].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
        playoutItems[5].GuideGroup.Should().Be(3);
        playoutItems[5].FillerKind.Should().Be(FillerKind.Tail);
        playoutItems[5].GuideFinish.HasValue.Should().BeFalse();

        playoutItems[6].MediaItemId.Should().Be(5);
        playoutItems[6].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 57, 0)));
        playoutItems[6].GuideGroup.Should().Be(3);
        playoutItems[6].FillerKind.Should().Be(FillerKind.Fallback);
        playoutItems[6].GuideFinish.HasValue.Should().BeFalse();
    }

    [Test]
    public void Should_Not_Have_Gap_With_Post_Roll_Pad_And_Fallback_Filler()
    {
        Collection collectionPre = TwoItemCollection(1, 2, TimeSpan.Parse("00:00:15.6734470"));
        Collection collectionOne = TwoItemCollection(3, 4, TimeSpan.Parse("00:22:58.1220000"));
        Collection collectionTwo = CollectionOf(
            new Dictionary<int, TimeSpan>
            {
                { 5, TimeSpan.Parse("00:00:31.3004760") },
                { 6, TimeSpan.Parse("00:00:31.7880950") },
                { 7, TimeSpan.Parse("00:00:31.1147170") },
                { 8, TimeSpan.Parse("00:00:46.4863270") },
                { 9, TimeSpan.Parse("00:00:31.4165760") },
                { 10, TimeSpan.Parse("00:00:31.5791160") },
                { 11, TimeSpan.Parse("00:00:31.2540360") },
                { 12, TimeSpan.Parse("00:00:36.2231070") },
                { 13, TimeSpan.Parse("00:02:00.0471430") }
            });
        Collection collectionThree = TwoItemCollection(14, 15, TimeSpan.Parse("00:00:55.6349890"));

        var scheduleItem = new ProgramScheduleItemDuration
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlayoutDuration = TimeSpan.FromMinutes(30),
            PlaybackOrder = PlaybackOrder.Chronological,
            PreRollFiller = new FillerPreset
            {
                FillerKind = FillerKind.PreRoll,
                FillerMode = FillerMode.Count,
                Count = 1,
                Collection = collectionPre,
                CollectionId = collectionPre.Id
            },
            PostRollFiller = new FillerPreset
            {
                FillerKind = FillerKind.PostRoll,
                FillerMode = FillerMode.Pad,
                PadToNearestMinute = 30,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            FallbackFiller = new FillerPreset
            {
                FillerKind = FillerKind.Fallback,
                Collection = collectionThree,
                CollectionId = collectionThree.Id
            }
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            new List<ProgramScheduleItem> { scheduleItem },
            new CollectionEnumeratorState());

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionPre.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator3 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var enumerator4 = new ChronologicalMediaCollectionEnumerator(
            collectionThree.MediaItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerDuration(_logger);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(
                scheduleItem,
                enumerator2,
                scheduleItem.PreRollFiller,
                enumerator1,
                scheduleItem.PostRollFiller,
                enumerator3,
                scheduleItem.FallbackFiller,
                enumerator4),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddMinutes(30));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        // THIS IS THE KEY TEST - needs to be exactly 30 minutes
        (playoutItems.Last().FinishOffset - playoutItems.First().StartOffset).Should().Be(TimeSpan.FromMinutes(30));

        // playoutBuilderState.NextGuideGroup.Should().Be(3);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(0);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(1);
        enumerator3.State.Index.Should().Be(0);
        enumerator4.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(12);
    }

    [Test]
    public void Should_Not_Schedule_At_HardStop()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));

        var scheduleItem = new ProgramScheduleItemDuration
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = TimeSpan.FromHours(6),
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = null,
            PlayoutDuration = TimeSpan.FromHours(1)
        };

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerDuration(_logger);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutItems.Should().BeEmpty();

        playoutBuilderState.CurrentTime.Should().Be(HardStop(scheduleItemsEnumerator));

        playoutBuilderState.NextGuideGroup.Should().Be(1);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(0);

        enumerator.State.Index.Should().Be(0);
    }
}
