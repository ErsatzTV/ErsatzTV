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
    public class PlayoutModeSchedulerDurationTests : SchedulerTestBase
    {
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
                PlaybackOrder = PlaybackOrder.Chronological
            };

            var enumerator = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());
            
            var scheduler = new PlayoutModeSchedulerDuration(new Mock<ILogger>().Object);
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator),
                scheduleItem,
                NextScheduleItem,
                HardStop);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.NextGuideGroup.Should().Be(4);
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);

            enumerator.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(3);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].GuideGroup.Should().Be(1);
            playoutItems[0].FillerKind.Should().Be(FillerKind.None);
            playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddHours(1));
            playoutItems[1].GuideGroup.Should().Be(2);
            playoutItems[1].FillerKind.Should().Be(FillerKind.None);
            playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.AddHours(2));
            playoutItems[2].GuideGroup.Should().Be(3);
            playoutItems[2].FillerKind.Should().Be(FillerKind.None);
            playoutItems[2].GuideFinish.HasValue.Should().BeTrue();
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

            var enumerator = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());
            
            var scheduler = new PlayoutModeSchedulerDuration(new Mock<ILogger>().Object);
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator),
                scheduleItem,
                NextScheduleItem,
                HardStop);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.NextGuideGroup.Should().Be(4);
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);
            
            enumerator.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(3);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].GuideGroup.Should().Be(1);
            playoutItems[0].FillerKind.Should().Be(FillerKind.None);
            playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].GuideGroup.Should().Be(2);
            playoutItems[1].FillerKind.Should().Be(FillerKind.None);
            playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
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

            var enumerator = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());
            
            var scheduler = new PlayoutModeSchedulerDuration(new Mock<ILogger>().Object);
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator),
                scheduleItem,
                NextScheduleItem,
                HardStop);

            // duration block should end after exact duration, with gap
            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));

            playoutBuilderState.NextGuideGroup.Should().Be(4);
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);
            
            enumerator.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(3);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].GuideGroup.Should().Be(1);
            playoutItems[0].FillerKind.Should().Be(FillerKind.None);
            playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].GuideGroup.Should().Be(2);
            playoutItems[1].FillerKind.Should().Be(FillerKind.None);
            playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
            playoutItems[2].GuideGroup.Should().Be(3);
            playoutItems[2].FillerKind.Should().Be(FillerKind.None);
            playoutItems[2].GuideFinish.HasValue.Should().BeTrue();
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

            var enumerator1 = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());
            
            var enumerator2 = new ChronologicalMediaCollectionEnumerator(
                collectionTwo.MediaItems,
                new CollectionEnumeratorState());
            
            var scheduler = new PlayoutModeSchedulerDuration(new Mock<ILogger>().Object);
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.FallbackFiller, enumerator2),
                scheduleItem,
                NextScheduleItem,
                HardStop);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.NextGuideGroup.Should().Be(4);
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
            playoutItems[0].GuideGroup.Should().Be(1);
            playoutItems[0].FillerKind.Should().Be(FillerKind.None);
            playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].GuideGroup.Should().Be(2);
            playoutItems[1].FillerKind.Should().Be(FillerKind.None);
            playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
            playoutItems[2].GuideGroup.Should().Be(3);
            playoutItems[2].FillerKind.Should().Be(FillerKind.None);
            playoutItems[2].GuideFinish.HasValue.Should().BeTrue();
            
            playoutItems[3].MediaItemId.Should().Be(3);
            playoutItems[3].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
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

            var enumerator1 = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());

            var enumerator2 = new ChronologicalMediaCollectionEnumerator(
                collectionTwo.MediaItems,
                new CollectionEnumeratorState());
            
            var scheduler = new PlayoutModeSchedulerDuration(new Mock<ILogger>().Object);
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
                scheduleItem,
                NextScheduleItem,
                HardStop);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.NextGuideGroup.Should().Be(4);
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);
            
            enumerator1.State.Index.Should().Be(1);
            enumerator2.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(6);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].GuideGroup.Should().Be(1);
            playoutItems[0].FillerKind.Should().Be(FillerKind.None);
            playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].GuideGroup.Should().Be(2);
            playoutItems[1].FillerKind.Should().Be(FillerKind.None);
            playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
            playoutItems[2].GuideGroup.Should().Be(3);
            playoutItems[2].FillerKind.Should().Be(FillerKind.None);
            playoutItems[2].GuideFinish.HasValue.Should().BeTrue();

            playoutItems[3].MediaItemId.Should().Be(3);
            playoutItems[3].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems[3].GuideGroup.Should().Be(3);
            playoutItems[3].FillerKind.Should().Be(FillerKind.Tail);
            playoutItems[3].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[4].MediaItemId.Should().Be(4);
            playoutItems[4].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 50, 0)));
            playoutItems[4].GuideGroup.Should().Be(3);
            playoutItems[4].FillerKind.Should().Be(FillerKind.Tail);
            playoutItems[3].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[5].MediaItemId.Should().Be(3);
            playoutItems[5].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 55, 0)));
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

            var enumerator1 = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());

            var enumerator2 = new ChronologicalMediaCollectionEnumerator(
                collectionTwo.MediaItems,
                new CollectionEnumeratorState());
            
            var scheduler = new PlayoutModeSchedulerDuration(new Mock<ILogger>().Object);
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
                scheduleItem,
                NextScheduleItem,
                HardStop);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 57, 0)));

            playoutBuilderState.NextGuideGroup.Should().Be(4);
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);
            
            enumerator1.State.Index.Should().Be(1);
            enumerator2.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(6);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].GuideGroup.Should().Be(1);
            playoutItems[0].FillerKind.Should().Be(FillerKind.None);
            playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].GuideGroup.Should().Be(2);
            playoutItems[1].FillerKind.Should().Be(FillerKind.None);
            playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
            playoutItems[2].GuideGroup.Should().Be(3);
            playoutItems[2].FillerKind.Should().Be(FillerKind.None);
            playoutItems[2].GuideFinish.HasValue.Should().BeTrue();

            playoutItems[3].MediaItemId.Should().Be(3);
            playoutItems[3].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems[3].GuideGroup.Should().Be(3);
            playoutItems[3].FillerKind.Should().Be(FillerKind.Tail);
            playoutItems[3].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[4].MediaItemId.Should().Be(4);
            playoutItems[4].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
            playoutItems[4].GuideGroup.Should().Be(3);
            playoutItems[4].FillerKind.Should().Be(FillerKind.Tail);
            playoutItems[4].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[5].MediaItemId.Should().Be(3);
            playoutItems[5].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
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

            var enumerator1 = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());

            var enumerator2 = new ChronologicalMediaCollectionEnumerator(
                collectionTwo.MediaItems,
                new CollectionEnumeratorState());

            var enumerator3 = new ChronologicalMediaCollectionEnumerator(
                collectionThree.MediaItems,
                new CollectionEnumeratorState());
            
            var scheduler = new PlayoutModeSchedulerDuration(new Mock<ILogger>().Object);
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
                HardStop);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.NextGuideGroup.Should().Be(4);
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);
            
            enumerator1.State.Index.Should().Be(1);
            enumerator2.State.Index.Should().Be(1);
            enumerator3.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(7);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].GuideGroup.Should().Be(1);
            playoutItems[0].FillerKind.Should().Be(FillerKind.None);
            playoutItems[0].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].GuideGroup.Should().Be(2);
            playoutItems[1].FillerKind.Should().Be(FillerKind.None);
            playoutItems[1].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
            playoutItems[2].GuideGroup.Should().Be(3);
            playoutItems[2].FillerKind.Should().Be(FillerKind.None);
            playoutItems[2].GuideFinish.HasValue.Should().BeTrue();

            playoutItems[3].MediaItemId.Should().Be(3);
            playoutItems[3].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems[3].GuideGroup.Should().Be(3);
            playoutItems[3].FillerKind.Should().Be(FillerKind.Tail);
            playoutItems[3].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[4].MediaItemId.Should().Be(4);
            playoutItems[4].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
            playoutItems[4].GuideGroup.Should().Be(3);
            playoutItems[4].FillerKind.Should().Be(FillerKind.Tail);
            playoutItems[4].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[5].MediaItemId.Should().Be(3);
            playoutItems[5].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
            playoutItems[5].GuideGroup.Should().Be(3);
            playoutItems[5].FillerKind.Should().Be(FillerKind.Tail);
            playoutItems[5].GuideFinish.HasValue.Should().BeFalse();

            playoutItems[6].MediaItemId.Should().Be(5);
            playoutItems[6].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 57, 0)));
            playoutItems[6].GuideGroup.Should().Be(3);
            playoutItems[6].FillerKind.Should().Be(FillerKind.Fallback);
            playoutItems[6].GuideFinish.HasValue.Should().BeFalse();
        }
    }
}
