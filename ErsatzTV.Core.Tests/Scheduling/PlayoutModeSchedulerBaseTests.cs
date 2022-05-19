using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class PlayoutModeSchedulerBaseTests : SchedulerTestBase
{
    [TestFixture]
    public class CalculateEndTimeWithFiller
    {
        [Test]
        public void Should_Not_Touch_Enumerator()
        {
            var collection = new Collection
            {
                Id = 1,
                Name = "Filler Items",
                MediaItems = new List<MediaItem>()
            };

            for (var i = 0; i < 5; i++)
            {
                collection.MediaItems.Add(TestMovie(i + 1, TimeSpan.FromHours(i + 1), new DateTime(2020, 2, i + 1)));
            }

            var fillerPreset = new FillerPreset
            {
                FillerKind = FillerKind.PreRoll,
                FillerMode = FillerMode.Count,
                Count = 3,
                Collection = collection,
                CollectionId = collection.Id
            };

            var enumerator = new ChronologicalMediaCollectionEnumerator(
                collection.MediaItems,
                new CollectionEnumeratorState { Index = 0, Seed = 1 });

            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>
                    {
                        { CollectionKey.ForFillerPreset(fillerPreset), enumerator }
                    },
                    new ProgramScheduleItemOne
                    {
                        PreRollFiller = fillerPreset
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 0, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30),
                    new List<MediaChapter>());

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 18, 12, 30, TimeSpan.FromHours(-5)));
            enumerator.State.Index.Should().Be(0);
            enumerator.State.Seed.Should().Be(1);
        }

        [Test]
        public void Should_Pad_To_15_Minutes_15()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 15
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 0, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30),
                    new List<MediaChapter>());

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 12, 15, 0, TimeSpan.FromHours(-5)));
        }

        [Test]
        public void Should_Pad_To_15_Minutes_30()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 15
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 16, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30),
                    new List<MediaChapter>());

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 12, 30, 0, TimeSpan.FromHours(-5)));
        }

        [Test]
        public void Should_Pad_To_15_Minutes_45()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 15
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 30, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30),
                    new List<MediaChapter>());

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 12, 45, 0, TimeSpan.FromHours(-5)));
        }

        [Test]
        public void Should_Pad_To_15_Minutes_00()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 15
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 46, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30),
                    new List<MediaChapter>());

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 13, 0, 0, TimeSpan.FromHours(-5)));
        }

        [Test]
        public void Should_Pad_To_30_Minutes_30()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 30
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 0, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30),
                    new List<MediaChapter>());

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 12, 30, 0, TimeSpan.FromHours(-5)));
        }

        [Test]
        public void Should_Pad_To_30_Minutes_00()
        {
            DateTimeOffset result = PlayoutModeSchedulerBase<ProgramScheduleItem>
                .CalculateEndTimeWithFiller(
                    new Dictionary<CollectionKey, IMediaCollectionEnumerator>(),
                    new ProgramScheduleItemOne
                    {
                        MidRollFiller = new FillerPreset
                        {
                            FillerKind = FillerKind.MidRoll,
                            FillerMode = FillerMode.Pad,
                            PadToNearestMinute = 30
                        }
                    },
                    new DateTimeOffset(2020, 2, 1, 12, 20, 0, TimeSpan.FromHours(-5)),
                    new TimeSpan(0, 12, 30),
                    new List<MediaChapter>());

            result.Should().Be(new DateTimeOffset(2020, 2, 1, 13, 0, 0, TimeSpan.FromHours(-5)));
        }
    }

    [TestFixture]
    public class AddFiller
    {
        [Test]
        public void Should_Not_Crash_Mid_Roll_Zero_Chapters()
        {
            Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));

            var scheduleItem = new ProgramScheduleItemOne
            {
                MidRollFiller = new FillerPreset
                {
                    FillerKind = FillerKind.MidRoll,
                    FillerMode = FillerMode.Pad,
                    PadToNearestMinute = 15
                }
            };

            var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
                new List<ProgramScheduleItem> { scheduleItem },
                new CollectionEnumeratorState());

            var enumerator = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());

            PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

            List<PlayoutItem> playoutItems = Scheduler()
                .AddFiller(
                    startState,
                    CollectionEnumerators(scheduleItem, enumerator),
                    scheduleItem,
                    new PlayoutItem(),
                    new List<MediaChapter>());

            playoutItems.Count.Should().Be(1);
        }

        [Test]
        public void Should_Not_Crash_Mid_Roll_One_Chapter()
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
                FallbackFiller = null,
                MidRollFiller = new FillerPreset
                {
                    FillerKind = FillerKind.MidRoll,
                    FillerMode = FillerMode.Pad,
                    PadToNearestMinute = 15
                }
            };

            var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
                new List<ProgramScheduleItem> { scheduleItem },
                new CollectionEnumeratorState());

            var enumerator = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());

            PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

            Dictionary<CollectionKey, IMediaCollectionEnumerator> enumerators = CollectionEnumerators(
                scheduleItem,
                enumerator);

            // too lazy to make another enumerator for the filler that we don't want
            enumerators.Add(CollectionKey.ForFillerPreset(scheduleItem.MidRollFiller), enumerator);

            List<PlayoutItem> playoutItems = Scheduler()
                .AddFiller(
                    startState,
                    enumerators,
                    scheduleItem,
                    new PlayoutItem(),
                    new List<MediaChapter> { new() });

            playoutItems.Count.Should().Be(1);
        }

        [Test]
        public void Should_Schedule_Mid_Roll_Count_Filler_Correctly()
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
                FallbackFiller = null,
                MidRollFiller = new FillerPreset
                {
                    FillerKind = FillerKind.MidRoll,
                    FillerMode = FillerMode.Count,
                    PadToNearestMinute = 60, // this should be ignored
                    Count = 1
                }
            };

            var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
                new List<ProgramScheduleItem> { scheduleItem },
                new CollectionEnumeratorState());

            var enumerator = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());

            var fillerEnumerator = new ChronologicalMediaCollectionEnumerator(
                collectionTwo.MediaItems,
                new CollectionEnumeratorState());

            PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

            Dictionary<CollectionKey, IMediaCollectionEnumerator> enumerators = CollectionEnumerators(
                scheduleItem,
                enumerator);

            enumerators.Add(CollectionKey.ForFillerPreset(scheduleItem.MidRollFiller), fillerEnumerator);

            List<PlayoutItem> playoutItems = Scheduler()
                .AddFiller(
                    startState,
                    enumerators,
                    scheduleItem,
                    new PlayoutItem
                    {
                        MediaItemId = 1,
                        Start = startState.CurrentTime.UtcDateTime,
                        Finish = startState.CurrentTime.AddHours(1).UtcDateTime
                    },
                    new List<MediaChapter>
                    {
                        new() { StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromMinutes(6) },
                        new() { StartTime = TimeSpan.FromMinutes(6), EndTime = TimeSpan.FromMinutes(60) }
                    });

            playoutItems.Count.Should().Be(3);
            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
            playoutItems[1].MediaItemId.Should().Be(3);
            playoutItems[1].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(6));
            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(11));
        }
    }

    [TestFixture]
    public class GetStartTimeAfter
    {
        [Test]
        public void Should_Compare_Time_As_Local_Time()
        {
            var enumerator = new Mock<IScheduleItemsEnumerator>();

            var state = new PlayoutBuilderState(
                enumerator.Object,
                None,
                None,
                false,
                false,
                0,
                DateTime.Today.AddHours(6).ToUniversalTime());

            var scheduleItem = new ProgramScheduleItemOne
            {
                StartTime = TimeSpan.FromHours(6)
            };

            DateTimeOffset result =
                PlayoutModeSchedulerBase<ProgramScheduleItem>.GetStartTimeAfter(state, scheduleItem);

            result.Should().Be(DateTime.Today.AddHours(6));
        }
    }

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

    private static PlayoutModeSchedulerBase<ProgramScheduleItem> Scheduler() =>
        new TestScheduler();

    private class TestScheduler : PlayoutModeSchedulerBase<ProgramScheduleItem>
    {
        private static readonly ILoggerFactory LoggerFactory;

        static TestScheduler()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger);
        }

        public TestScheduler() : base(LoggerFactory.CreateLogger<TestScheduler>())
        {
        }

        public override Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            ProgramScheduleItem scheduleItem,
            ProgramScheduleItem nextScheduleItem,
            DateTimeOffset hardStop) =>
            throw new NotSupportedException();
    }
}
