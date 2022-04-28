using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class PlayoutModeSchedulerFloodTests : SchedulerTestBase
{
    [Test]
    public void Should_Fill_Exactly_To_Next_Schedule_Item()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = null
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

        var scheduler = new PlayoutModeSchedulerFlood(new Mock<ILogger>().Object);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator));

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(1);

        enumerator.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(3);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddHours(1));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.AddHours(2));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
    }

    [Test]
    public void Should_Fill_Exactly_To_Next_Schedule_Item_Flood()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = null
        };

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            // this caused trouble with the peek logic and the IsFlood flag
            new ProgramScheduleItemFlood
            {
                StartTime = TimeSpan.FromHours(3)
            }
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(new Mock<ILogger>().Object);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator));

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(1);

        enumerator.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(3);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddHours(1));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.AddHours(2));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
    }

    [Test]
    public void Should_Fill_Exactly_To_Next_Schedule_Item_With_Post_Roll_Multiple_One()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            PostRollFiller = new FillerPreset
            {
                FillerKind = FillerKind.PostRoll,
                FillerMode = FillerMode.Count,
                Count = 1,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            TailFiller = null,
            FallbackFiller = null
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
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

        var scheduler = new PlayoutModeSchedulerFlood(new Mock<ILogger>().Object);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.PostRollFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator));

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(1);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(6);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);

        playoutItems[1].MediaItemId.Should().Be(3);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(1);
        playoutItems[1].FillerKind.Should().Be(FillerKind.PostRoll);

        playoutItems[2].MediaItemId.Should().Be(2);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.AddHours(1));
        playoutItems[2].GuideGroup.Should().Be(2);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);

        playoutItems[3].MediaItemId.Should().Be(4);
        playoutItems[3].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 55, 0)));
        playoutItems[3].GuideGroup.Should().Be(2);
        playoutItems[3].FillerKind.Should().Be(FillerKind.PostRoll);

        playoutItems[4].MediaItemId.Should().Be(1);
        playoutItems[4].StartOffset.Should().Be(startState.CurrentTime.AddHours(2));
        playoutItems[4].GuideGroup.Should().Be(3);
        playoutItems[4].FillerKind.Should().Be(FillerKind.None);

        playoutItems[5].MediaItemId.Should().Be(3);
        playoutItems[5].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 55, 0)));
        playoutItems[5].GuideGroup.Should().Be(3);
        playoutItems[5].FillerKind.Should().Be(FillerKind.PostRoll);
    }

    [Test]
    public void Should_Have_Gap_With_No_Tail_No_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = null
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

        var scheduler = new PlayoutModeSchedulerFlood(new Mock<ILogger>().Object);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator));

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(1);

        enumerator.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(3);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
    }

    [Test]
    public void Should_Not_Have_Gap_With_Exact_Tail()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            FallbackFiller = null
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
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

        var scheduler = new PlayoutModeSchedulerFlood(new Mock<ILogger>().Object);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator));

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(1);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(6);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);

        playoutItems[3].MediaItemId.Should().Be(3);
        playoutItems[3].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.Should().Be(3);
        playoutItems[3].FillerKind.Should().Be(FillerKind.Tail);

        playoutItems[4].MediaItemId.Should().Be(4);
        playoutItems[4].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 50, 0)));
        playoutItems[4].GuideGroup.Should().Be(3);
        playoutItems[4].FillerKind.Should().Be(FillerKind.Tail);

        playoutItems[5].MediaItemId.Should().Be(3);
        playoutItems[5].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 55, 0)));
        playoutItems[5].GuideGroup.Should().Be(3);
        playoutItems[5].FillerKind.Should().Be(FillerKind.Tail);
    }

    [Test]
    public void Should_Not_Have_Gap_With_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            }
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
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

        var scheduler = new PlayoutModeSchedulerFlood(new Mock<ILogger>().Object);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.FallbackFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator));

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(1);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(4);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);

        playoutItems[3].MediaItemId.Should().Be(3);
        playoutItems[3].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.Should().Be(3);
        playoutItems[3].FillerKind.Should().Be(FillerKind.Fallback);
    }

    [Test]
    public void Should_Have_Gap_With_Tail_No_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            FallbackFiller = null
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
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

        var scheduler = new PlayoutModeSchedulerFlood(new Mock<ILogger>().Object);
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator));

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 57, 0)));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(1);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(6);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);

        playoutItems[3].MediaItemId.Should().Be(3);
        playoutItems[3].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.Should().Be(3);
        playoutItems[3].FillerKind.Should().Be(FillerKind.Tail);

        playoutItems[4].MediaItemId.Should().Be(4);
        playoutItems[4].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
        playoutItems[4].GuideGroup.Should().Be(3);
        playoutItems[4].FillerKind.Should().Be(FillerKind.Tail);

        playoutItems[5].MediaItemId.Should().Be(3);
        playoutItems[5].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
        playoutItems[5].GuideGroup.Should().Be(3);
        playoutItems[5].FillerKind.Should().Be(FillerKind.Tail);
    }

    [Test]
    public void Should_Not_Have_Gap_With_Tail_And_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));
        Collection collectionThree = TwoItemCollection(5, 6, TimeSpan.FromMinutes(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
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

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var enumerator3 = new ChronologicalMediaCollectionEnumerator(
            collectionThree.MediaItems,
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

        var scheduler = new PlayoutModeSchedulerFlood(new Mock<ILogger>().Object);
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
            HardStop(scheduleItemsEnumerator));

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(1);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(1);
        enumerator3.State.Index.Should().Be(1);

        playoutItems.Count.Should().Be(7);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);

        playoutItems[3].MediaItemId.Should().Be(3);
        playoutItems[3].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.Should().Be(3);
        playoutItems[3].FillerKind.Should().Be(FillerKind.Tail);

        playoutItems[4].MediaItemId.Should().Be(4);
        playoutItems[4].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
        playoutItems[4].GuideGroup.Should().Be(3);
        playoutItems[4].FillerKind.Should().Be(FillerKind.Tail);

        playoutItems[5].MediaItemId.Should().Be(3);
        playoutItems[5].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
        playoutItems[5].GuideGroup.Should().Be(3);
        playoutItems[5].FillerKind.Should().Be(FillerKind.Tail);

        playoutItems[6].MediaItemId.Should().Be(5);
        playoutItems[6].StartOffset.Should().Be(startState.CurrentTime.Add(new TimeSpan(2, 57, 0)));
        playoutItems[6].GuideGroup.Should().Be(3);
        playoutItems[6].FillerKind.Should().Be(FillerKind.Fallback);
    }

    [Test]
    public void Should_Not_Have_Gap_With_Unused_Tail_And_Unused_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));
        Collection collectionThree = TwoItemCollection(5, 6, TimeSpan.FromMinutes(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
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

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var enumerator3 = new ChronologicalMediaCollectionEnumerator(
            collectionThree.MediaItems,
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

        var scheduler = new PlayoutModeSchedulerFlood(new Mock<ILogger>().Object);
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
            HardStop(scheduleItemsEnumerator));

        playoutBuilderState.CurrentTime.Should().Be(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.Should().Be(4);
        playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
        playoutBuilderState.InFlood.Should().BeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
        playoutBuilderState.InDurationFiller.Should().BeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.Should().Be(1);

        enumerator1.State.Index.Should().Be(1);
        enumerator2.State.Index.Should().Be(0);
        enumerator3.State.Index.Should().Be(0);

        playoutItems.Count.Should().Be(3);

        playoutItems[0].MediaItemId.Should().Be(1);
        playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
        playoutItems[0].GuideGroup.Should().Be(1);
        playoutItems[0].FillerKind.Should().Be(FillerKind.None);

        playoutItems[1].MediaItemId.Should().Be(2);
        playoutItems[1].StartOffset.Should().Be(startState.CurrentTime.AddHours(1));
        playoutItems[1].GuideGroup.Should().Be(2);
        playoutItems[1].FillerKind.Should().Be(FillerKind.None);

        playoutItems[2].MediaItemId.Should().Be(1);
        playoutItems[2].StartOffset.Should().Be(startState.CurrentTime.AddHours(2));
        playoutItems[2].GuideGroup.Should().Be(3);
        playoutItems[2].FillerKind.Should().Be(FillerKind.None);
    }

    protected override ProgramScheduleItem NextScheduleItem => new ProgramScheduleItemOne
    {
        StartTime = TimeSpan.FromHours(3)
    };
}
