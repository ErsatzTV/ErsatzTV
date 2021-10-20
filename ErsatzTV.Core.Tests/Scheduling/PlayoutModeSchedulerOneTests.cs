using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling
{
    [TestFixture]
    public class PlayoutModeSchedulerOneTests : SchedulerTestBase
    {
        [Test]
        public void Should_Have_Gap_With_No_Tail_No_Fallback()
        {
            Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));

            var scheduleItem = new ProgramScheduleItemOne
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

            var scheduler = new PlayoutModeSchedulerOne();
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator),
                scheduleItem,
                NextScheduleItem,
                HardStop,
                new Mock<ILogger>().Object);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(1));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.CustomGroup.Should().BeFalse();
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);

            enumerator.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(1);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeFalse();
            playoutItems[0].IsFiller.Should().BeFalse();
        }

        [Test]
        public void Should_Not_Have_Gap_With_Exact_Tail()
        {
            Collection collectionOne = TwoItemCollection(1, 2, new TimeSpan(2, 45, 0));
            Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));

            var scheduleItem = new ProgramScheduleItemOne
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

            var scheduler = new PlayoutModeSchedulerOne();
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
                scheduleItem,
                NextScheduleItem,
                HardStop,
                new Mock<ILogger>().Object);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.CustomGroup.Should().BeFalse();
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);

            enumerator1.State.Index.Should().Be(1);
            enumerator2.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(4);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeFalse();
            playoutItems[0].IsFiller.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(3);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems[1].CustomGroup.Should().BeTrue();
            playoutItems[1].IsFiller.Should().BeTrue();

            playoutItems[2].MediaItemId.Should().Be(4);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 50, 0)));
            playoutItems[2].CustomGroup.Should().BeTrue();
            playoutItems[2].IsFiller.Should().BeTrue();
        
            playoutItems[3].MediaItemId.Should().Be(3);
            playoutItems[3].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 55, 0)));
            playoutItems[3].CustomGroup.Should().BeTrue();
            playoutItems[3].IsFiller.Should().BeTrue();
        }

        [Test]
        public void Should_Not_Have_Gap_With_Fallback()
        {
            Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));
            Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));

            var scheduleItem = new ProgramScheduleItemOne
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

            var scheduler = new PlayoutModeSchedulerOne();
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.FallbackFiller, enumerator2),
                scheduleItem,
                NextScheduleItem,
                HardStop,
                new Mock<ILogger>().Object);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.CustomGroup.Should().BeFalse();
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);

            enumerator1.State.Index.Should().Be(1);
            enumerator2.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(2);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeFalse();
            playoutItems[0].IsFiller.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(3);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddHours(1));
            playoutItems[1].CustomGroup.Should().BeTrue();
            playoutItems[1].IsFiller.Should().BeTrue();
        }
        
        [Test]
        public void Should_Have_Gap_With_Tail_No_Fallback()
        {
            Collection collectionOne = TwoItemCollection(1, 2, new TimeSpan(2, 45, 0));
            Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));

            var scheduleItem = new ProgramScheduleItemOne
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

            var scheduler = new PlayoutModeSchedulerOne();
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
                scheduleItem,
                NextScheduleItem,
                HardStop,
                new Mock<ILogger>().Object);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 57, 0)));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.CustomGroup.Should().BeFalse();
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);

            enumerator1.State.Index.Should().Be(1);
            enumerator2.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(4);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeFalse();
            playoutItems[0].IsFiller.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(3);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems[1].CustomGroup.Should().BeTrue();
            playoutItems[1].IsFiller.Should().BeTrue();

            playoutItems[2].MediaItemId.Should().Be(4);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
            playoutItems[2].CustomGroup.Should().BeTrue();
            playoutItems[2].IsFiller.Should().BeTrue();
        
            playoutItems[3].MediaItemId.Should().Be(3);
            playoutItems[3].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
            playoutItems[3].CustomGroup.Should().BeTrue();
            playoutItems[3].IsFiller.Should().BeTrue();
        }

        [Test]
        public void Should_Not_Have_Gap_With_Tail_And_Fallback()
        {
            Collection collectionOne = TwoItemCollection(1, 2, new TimeSpan(2, 45, 0));
            Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));
            Collection collectionThree = TwoItemCollection(5, 6, TimeSpan.FromMinutes(1));

            var scheduleItem = new ProgramScheduleItemOne
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

            var scheduler = new PlayoutModeSchedulerOne();
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(
                    scheduleItem,
                    enumerator1,
                    scheduleItem.TailFiller,
                    enumerator2,
                    scheduleItem.FallbackFiller,
                    enumerator3),
                scheduleItem,
                NextScheduleItem,
                HardStop,
                new Mock<ILogger>().Object);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.CustomGroup.Should().BeFalse();
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);

            enumerator1.State.Index.Should().Be(1);
            enumerator2.State.Index.Should().Be(1);
            enumerator3.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(5);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeFalse();
            playoutItems[0].IsFiller.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(3);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems[1].CustomGroup.Should().BeTrue();
            playoutItems[1].IsFiller.Should().BeTrue();

            playoutItems[2].MediaItemId.Should().Be(4);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
            playoutItems[2].CustomGroup.Should().BeTrue();
            playoutItems[2].IsFiller.Should().BeTrue();
        
            playoutItems[3].MediaItemId.Should().Be(3);
            playoutItems[3].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
            playoutItems[3].CustomGroup.Should().BeTrue();
            playoutItems[3].IsFiller.Should().BeTrue();

            playoutItems[4].MediaItemId.Should().Be(5);
            playoutItems[4].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 57, 0)));
            playoutItems[4].CustomGroup.Should().BeTrue();
            playoutItems[4].IsFiller.Should().BeTrue();
        }

        [Test]
        public void Should_Not_Have_Gap_With_Unused_Tail_And_Unused_Fallback()
        {
            Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(3));
            Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));
            Collection collectionThree = TwoItemCollection(5, 6, TimeSpan.FromMinutes(1));

            var scheduleItem = new ProgramScheduleItemOne
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

            var scheduler = new PlayoutModeSchedulerOne();
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(
                    scheduleItem,
                    enumerator1,
                    scheduleItem.TailFiller,
                    enumerator2,
                    scheduleItem.FallbackFiller,
                    enumerator3),
                scheduleItem,
                NextScheduleItem,
                HardStop,
                new Mock<ILogger>().Object);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.CustomGroup.Should().BeFalse();
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);

            enumerator1.State.Index.Should().Be(1);
            enumerator2.State.Index.Should().Be(0);
            enumerator3.State.Index.Should().Be(0);

            playoutItems.Count.Should().Be(1);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeFalse();
            playoutItems[0].IsFiller.Should().BeFalse();
        }

        protected override ProgramScheduleItem NextScheduleItem => new ProgramScheduleItemOne
        {
            StartTime = TimeSpan.FromHours(3)
        };
    }
}
