using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Tests.Scheduling
{
    [TestFixture]
    public class PlayoutModeSchedulerDurationTests
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
            
            var scheduler = new PlayoutModeSchedulerDuration();
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator),
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

            enumerator.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(3);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeTrue();
            playoutItems[0].IsFiller.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddHours(1));
            playoutItems[1].CustomGroup.Should().BeTrue();
            playoutItems[1].IsFiller.Should().BeFalse();
        
            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.AddHours(2));
            playoutItems[2].CustomGroup.Should().BeTrue();
            playoutItems[2].IsFiller.Should().BeFalse();
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
            
            var scheduler = new PlayoutModeSchedulerDuration();
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator),
                scheduleItem,
                NextScheduleItem,
                HardStop,
                new Mock<ILogger>().Object);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems.Last().FinishOffset.Should().Be(playoutBuilderState.CurrentTime);

            playoutBuilderState.CustomGroup.Should().BeFalse();
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);
            
            enumerator.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(3);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeTrue();
            playoutItems[0].IsFiller.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].CustomGroup.Should().BeTrue();
            playoutItems[1].IsFiller.Should().BeFalse();
        
            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
            playoutItems[2].CustomGroup.Should().BeTrue();
            playoutItems[2].IsFiller.Should().BeFalse();
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
            
            var scheduler = new PlayoutModeSchedulerDuration();
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator),
                scheduleItem,
                NextScheduleItem,
                HardStop,
                new Mock<ILogger>().Object);

            // duration block should end after exact duration, with gap
            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));

            playoutBuilderState.CustomGroup.Should().BeFalse();
            playoutBuilderState.DurationFinish.IsNone.Should().BeTrue();
            playoutBuilderState.InFlood.Should().BeFalse();
            playoutBuilderState.MultipleRemaining.IsNone.Should().BeTrue();
            playoutBuilderState.InDurationFiller.Should().BeFalse();
            playoutBuilderState.ScheduleItemIndex.Should().Be(1);
            
            enumerator.State.Index.Should().Be(1);

            playoutItems.Count.Should().Be(3);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeTrue();
            playoutItems[0].IsFiller.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].CustomGroup.Should().BeTrue();
            playoutItems[1].IsFiller.Should().BeFalse();
        
            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
            playoutItems[2].CustomGroup.Should().BeTrue();
            playoutItems[2].IsFiller.Should().BeFalse();
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
            
            var scheduler = new PlayoutModeSchedulerDuration();
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

            playoutItems.Count.Should().Be(6);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeTrue();
            playoutItems[0].IsFiller.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].CustomGroup.Should().BeTrue();
            playoutItems[1].IsFiller.Should().BeFalse();
        
            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
            playoutItems[2].CustomGroup.Should().BeTrue();
            playoutItems[2].IsFiller.Should().BeFalse();
            
            playoutItems[3].MediaItemId.Should().Be(3);
            playoutItems[3].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems[3].CustomGroup.Should().BeTrue();
            playoutItems[3].IsFiller.Should().BeTrue();

            playoutItems[4].MediaItemId.Should().Be(4);
            playoutItems[4].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 50, 0)));
            playoutItems[4].CustomGroup.Should().BeTrue();
            playoutItems[4].IsFiller.Should().BeTrue();
        
            playoutItems[5].MediaItemId.Should().Be(3);
            playoutItems[5].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 55, 0)));
            playoutItems[5].CustomGroup.Should().BeTrue();
            playoutItems[5].IsFiller.Should().BeTrue();
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
            
            var scheduler = new PlayoutModeSchedulerDuration();
            (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
                StartState,
                CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
                scheduleItem,
                NextScheduleItem,
                HardStop,
                new Mock<ILogger>().Object);

            playoutBuilderState.CurrentTime.Should().Be(StartState.CurrentTime.AddHours(3));
            playoutItems.Last().FinishOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 57, 0)));

            playoutBuilderState.CustomGroup.Should().BeFalse();
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
            playoutItems[0].CustomGroup.Should().BeTrue();
            playoutItems[0].IsFiller.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].CustomGroup.Should().BeTrue();
            playoutItems[1].IsFiller.Should().BeFalse();
        
            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
            playoutItems[2].CustomGroup.Should().BeTrue();
            playoutItems[2].IsFiller.Should().BeFalse();
            
            playoutItems[3].MediaItemId.Should().Be(3);
            playoutItems[3].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems[3].CustomGroup.Should().BeTrue();
            playoutItems[3].IsFiller.Should().BeTrue();

            playoutItems[4].MediaItemId.Should().Be(4);
            playoutItems[4].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
            playoutItems[4].CustomGroup.Should().BeTrue();
            playoutItems[4].IsFiller.Should().BeTrue();
        
            playoutItems[5].MediaItemId.Should().Be(3);
            playoutItems[5].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
            playoutItems[5].CustomGroup.Should().BeTrue();
            playoutItems[5].IsFiller.Should().BeTrue();
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
            
            var scheduler = new PlayoutModeSchedulerDuration();
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

            playoutItems.Count.Should().Be(7);

            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(StartState.CurrentTime);
            playoutItems[0].CustomGroup.Should().BeTrue();
            playoutItems[0].IsFiller.Should().BeFalse();

            playoutItems[1].MediaItemId.Should().Be(2);
            playoutItems[1].StartOffset.Should().Be(StartState.CurrentTime.AddMinutes(55));
            playoutItems[1].CustomGroup.Should().BeTrue();
            playoutItems[1].IsFiller.Should().BeFalse();
        
            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
            playoutItems[2].CustomGroup.Should().BeTrue();
            playoutItems[2].IsFiller.Should().BeFalse();
            
            playoutItems[3].MediaItemId.Should().Be(3);
            playoutItems[3].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
            playoutItems[3].CustomGroup.Should().BeTrue();
            playoutItems[3].IsFiller.Should().BeTrue();

            playoutItems[4].MediaItemId.Should().Be(4);
            playoutItems[4].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
            playoutItems[4].CustomGroup.Should().BeTrue();
            playoutItems[4].IsFiller.Should().BeTrue();
        
            playoutItems[5].MediaItemId.Should().Be(3);
            playoutItems[5].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
            playoutItems[5].CustomGroup.Should().BeTrue();
            playoutItems[5].IsFiller.Should().BeTrue();

            playoutItems[6].MediaItemId.Should().Be(5);
            playoutItems[6].StartOffset.Should().Be(StartState.CurrentTime.Add(new TimeSpan(2, 57, 0)));
            playoutItems[6].CustomGroup.Should().BeTrue();
            playoutItems[6].IsFiller.Should().BeTrue();
        }

        private static PlayoutBuilderState StartState => new(
            0,
            None,
            None,
            false,
            false,
            false,
            new DateTimeOffset(2020, 10, 18, 0, 0, 0, TimeSpan.FromHours(-5)));

        private static Dictionary<CollectionKey, IMediaCollectionEnumerator> CollectionEnumerators(
            ProgramScheduleItem scheduleItem, IMediaCollectionEnumerator enumerator) =>
            new()
            {
                { CollectionKey.ForScheduleItem(scheduleItem), enumerator }
            };
        
        private static Dictionary<CollectionKey, IMediaCollectionEnumerator> CollectionEnumerators(
            ProgramScheduleItem scheduleItem, IMediaCollectionEnumerator enumerator1,
            FillerPreset fillerPreset, IMediaCollectionEnumerator enumerator2) =>
            new()
            {
                { CollectionKey.ForScheduleItem(scheduleItem), enumerator1 },
                { CollectionKey.ForFillerPreset(fillerPreset), enumerator2 }
            };

        private static Dictionary<CollectionKey, IMediaCollectionEnumerator> CollectionEnumerators(
            ProgramScheduleItem scheduleItem, IMediaCollectionEnumerator enumerator1,
            FillerPreset fillerPreset, IMediaCollectionEnumerator enumerator2,
            FillerPreset fillerPreset2, IMediaCollectionEnumerator enumerator3) =>
            new()
            {
                { CollectionKey.ForScheduleItem(scheduleItem), enumerator1 },
                { CollectionKey.ForFillerPreset(fillerPreset), enumerator2 },
                { CollectionKey.ForFillerPreset(fillerPreset2), enumerator3 }
            };

        private static ProgramScheduleItem NextScheduleItem => new ProgramScheduleItemOne
        {
            StartTime = null
        };

        private static DateTimeOffset HardStop => StartState.CurrentTime.AddHours(6);
        
        private static Movie TestMovie(int id, TimeSpan duration, DateTime aired) =>
            new()
            {
                Id = id,
                MovieMetadata = new List<MovieMetadata> { new() { ReleaseDate = aired } },
                MediaVersions = new List<MediaVersion>
                {
                    new() { Duration = duration }
                }
            };

        private static Collection TwoItemCollection(int id1, int id2, TimeSpan duration) => new()
        {
            Id = id1,
            Name = $"Duration Items {id1}",
            MediaItems = new List<MediaItem>
            {
                TestMovie(id1, duration, new DateTime(2020, 1, 1)),
                TestMovie(id2, duration, new DateTime(2020, 1, 2))
            }
        };
    }
}
