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
    [SetUp]
    public void SetUp()
    {
        _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        _scheduler = new TestScheduler();
    }

    private CancellationToken _cancellationToken;
    private PlayoutModeSchedulerBase<ProgramScheduleItem> _scheduler;

    [TestFixture]
    public class AddFiller : PlayoutModeSchedulerBaseTests
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

            List<PlayoutItem> playoutItems = _scheduler
                .AddFiller(
                    startState,
                    CollectionEnumerators(scheduleItem, enumerator),
                    scheduleItem,
                    new PlayoutItem(),
                    new List<MediaChapter>(),
                    true,
                    _cancellationToken);

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

            List<PlayoutItem> playoutItems = _scheduler
                .AddFiller(
                    startState,
                    enumerators,
                    scheduleItem,
                    new PlayoutItem(),
                    new List<MediaChapter> { new() },
                    true,
                    _cancellationToken);

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

            List<PlayoutItem> playoutItems = _scheduler
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
                    },
                    true,
                    _cancellationToken);

            playoutItems.Count.Should().Be(3);
            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);
            playoutItems[1].MediaItemId.Should().Be(3);
            playoutItems[1].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(6));
            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(11));
        }

        [Test]
        public void Should_Schedule_Post_Roll_After_Padded_Mid_Roll()
        {
            // content 45 min, mid roll pad to 60, post roll 5 min
            // content + post = 50 min, mid roll will add two 5 min items
            // content + mid + post = 60 min

            Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(45));
            Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));
            Collection collectionThree = TwoItemCollection(5, 6, TimeSpan.FromMinutes(5));

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
                    PadToNearestMinute = 60,
                    CollectionId = 2,
                    Collection = collectionTwo
                },
                PostRollFiller = new FillerPreset
                {
                    FillerKind = FillerKind.PostRoll,
                    FillerMode = FillerMode.Count,
                    Count = 1,
                    CollectionId = 3,
                    Collection = collectionThree
                }
            };

            var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
                new List<ProgramScheduleItem> { scheduleItem },
                new CollectionEnumeratorState());

            var enumerator = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());

            var midRollFillerEnumerator = new ChronologicalMediaCollectionEnumerator(
                collectionTwo.MediaItems,
                new CollectionEnumeratorState());

            var postRollFillerEnumerator = new ChronologicalMediaCollectionEnumerator(
                collectionThree.MediaItems,
                new CollectionEnumeratorState());

            PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

            Dictionary<CollectionKey, IMediaCollectionEnumerator> enumerators = CollectionEnumerators(
                scheduleItem,
                enumerator);

            enumerators.Add(CollectionKey.ForFillerPreset(scheduleItem.MidRollFiller), midRollFillerEnumerator);
            enumerators.Add(CollectionKey.ForFillerPreset(scheduleItem.PostRollFiller), postRollFillerEnumerator);

            List<PlayoutItem> playoutItems = _scheduler
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
                        new() { StartTime = TimeSpan.FromMinutes(6), EndTime = TimeSpan.FromMinutes(45) }
                    },
                    true,
                    _cancellationToken);

            playoutItems.Count.Should().Be(5);

            // content chapter 1
            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);

            // mid-roll 1
            playoutItems[1].MediaItemId.Should().Be(3);
            playoutItems[1].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(6));

            // mid-roll 2
            playoutItems[2].MediaItemId.Should().Be(4);
            playoutItems[2].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(11));

            // content chapter 2
            playoutItems[3].MediaItemId.Should().Be(1);
            playoutItems[3].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(16));

            // post-roll
            playoutItems[4].MediaItemId.Should().Be(5);
            playoutItems[4].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(55));
        }

        [Test]
        public void Should_Schedule_Padded_Post_Roll_After_Mid_Roll_Count()
        {
            // content 45 min, mid roll 5 min, post roll pad to 60
            // content + mid = 50 min, post roll will add two 5 min items
            // content + mid + post = 60 min

            Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(45));
            Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));
            Collection collectionThree = TwoItemCollection(5, 6, TimeSpan.FromMinutes(5));

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
                    Count = 1,
                    CollectionId = 2,
                    Collection = collectionTwo
                },
                PostRollFiller = new FillerPreset
                {
                    FillerKind = FillerKind.PostRoll,
                    FillerMode = FillerMode.Pad,
                    PadToNearestMinute = 60,
                    CollectionId = 3,
                    Collection = collectionThree
                }
            };

            var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
                new List<ProgramScheduleItem> { scheduleItem },
                new CollectionEnumeratorState());

            var enumerator = new ChronologicalMediaCollectionEnumerator(
                collectionOne.MediaItems,
                new CollectionEnumeratorState());

            var midRollFillerEnumerator = new ChronologicalMediaCollectionEnumerator(
                collectionTwo.MediaItems,
                new CollectionEnumeratorState());

            var postRollFillerEnumerator = new ChronologicalMediaCollectionEnumerator(
                collectionThree.MediaItems,
                new CollectionEnumeratorState());

            PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

            Dictionary<CollectionKey, IMediaCollectionEnumerator> enumerators = CollectionEnumerators(
                scheduleItem,
                enumerator);

            enumerators.Add(CollectionKey.ForFillerPreset(scheduleItem.MidRollFiller), midRollFillerEnumerator);
            enumerators.Add(CollectionKey.ForFillerPreset(scheduleItem.PostRollFiller), postRollFillerEnumerator);

            List<PlayoutItem> playoutItems = _scheduler
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
                        new() { StartTime = TimeSpan.FromMinutes(6), EndTime = TimeSpan.FromMinutes(45) }
                    },
                    true,
                    _cancellationToken);

            playoutItems.Count.Should().Be(5);

            // content chapter 1
            playoutItems[0].MediaItemId.Should().Be(1);
            playoutItems[0].StartOffset.Should().Be(startState.CurrentTime);

            // mid-roll 1
            playoutItems[1].MediaItemId.Should().Be(3);
            playoutItems[1].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(6));

            // content chapter 2
            playoutItems[2].MediaItemId.Should().Be(1);
            playoutItems[2].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(11));

            // post-roll 1
            playoutItems[3].MediaItemId.Should().Be(5);
            playoutItems[3].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(50));

            // post-roll 2
            playoutItems[4].MediaItemId.Should().Be(6);
            playoutItems[4].StartOffset.Should().Be(startState.CurrentTime + TimeSpan.FromMinutes(55));
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
            DateTimeOffset hardStop,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
