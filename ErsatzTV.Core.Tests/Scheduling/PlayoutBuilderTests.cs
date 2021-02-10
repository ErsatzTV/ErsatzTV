using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Tests.Scheduling
{
    public class PlayoutBuilderTests
    {
        private readonly ILogger<PlayoutBuilder> _logger;

        public PlayoutBuilderTests()
        {
            if (Log.Logger.GetType().FullName == "Serilog.Core.Pipeline.SilentLogger")
            {
                Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
                Log.Logger.Debug(
                    "Logger is not configured. Either this is a unit test or you have to configure the logger");
            }

            ServiceProvider serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddSerilog(dispose: true))
                .BuildServiceProvider();

            ILoggerFactory factory = serviceProvider.GetService<ILoggerFactory>();

            _logger = factory.CreateLogger<PlayoutBuilder>();
        }

        [Test]
        public async Task InitialFlood_Should_StartAtMidnight()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today)
            };

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Random);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().Start.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items.Head().Finish.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
        }

        [Test]
        public async Task InitialFlood_Should_StartAtMidnight_With_LateStart()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today)
            };

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Random);
            DateTimeOffset start = HoursAfterMidnight(1);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(2);
            result.Items[0].Start.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[1].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[1].Finish.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
        }

        [Test]
        public async Task ChronologicalContent_Should_CreateChronologicalItems()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
            };

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(4);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(4);
            result.Items[0].Start.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(1);
            result.Items[3].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[3].MediaItemId.Should().Be(2);
        }

        [Test]
        public async Task ChronologicalFlood_Should_AnchorAndMaintainExistingPlayout()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today.AddHours(1))
            };

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().MediaItemId.Should().Be(1);

            result.Anchor.NextStart.Should().Be(DateTime.Today.AddHours(6));

            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);

            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            Playout result2 = await builder.BuildPlayoutItems(playout, start2, finish2);

            result2.Items.Count.Should().Be(2);
            result2.Items.Last().Start.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result2.Items.Last().MediaItemId.Should().Be(2);

            result2.Anchor.NextStart.Should().Be(DateTime.Today.AddHours(12));
            result2.ProgramScheduleAnchors.Count.Should().Be(1);
            result2.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);
        }

        [Test]
        public async Task ChronologicalFlood_Should_AnchorAndReturnNewPlayoutItems()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today.AddHours(1))
            };

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().MediaItemId.Should().Be(1);

            result.Anchor.NextStart.Should().Be(DateTime.Today.AddHours(6));
            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);

            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(12);

            Playout result2 = await builder.BuildPlayoutItems(playout, start2, finish2);

            result2.Items.Count.Should().Be(3);
            result2.Items[1].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result2.Items[1].MediaItemId.Should().Be(2);
            result2.Items[2].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result2.Items[2].MediaItemId.Should().Be(1);

            result2.Anchor.NextStart.Should().Be(DateTime.Today.AddHours(18));
            result2.ProgramScheduleAnchors.Count.Should().Be(1);
            result2.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);
        }

        [Test]
        public async Task ShuffleFloodRebuild_Should_IgnoreAnchors()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(1), DateTime.Today.AddHours(1)),
                TestMovie(3, TimeSpan.FromHours(1), DateTime.Today.AddHours(2)),
                TestMovie(4, TimeSpan.FromHours(1), DateTime.Today.AddHours(3)),
                TestMovie(5, TimeSpan.FromHours(1), DateTime.Today.AddHours(4)),
                TestMovie(6, TimeSpan.FromHours(1), DateTime.Today.AddHours(5))
            };

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Shuffle);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(6);
            result.Anchor.NextStart.Should().Be(DateTime.Today.AddHours(6));

            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);

            int firstSeedValue = result.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            Playout result2 = await builder.BuildPlayoutItems(playout, start2, finish2, true);

            result2.Items.Count.Should().Be(6);
            result2.Anchor.NextStart.Should().Be(DateTime.Today.AddHours(6));

            result2.ProgramScheduleAnchors.Count.Should().Be(1);
            result2.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);

            int secondSeedValue = result2.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            firstSeedValue.Should().NotBe(secondSeedValue);
        }

        [Test]
        public async Task ShuffleFlood_Should_MaintainRandomSeed()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(1), DateTime.Today.AddHours(1)),
                TestMovie(3, TimeSpan.FromHours(1), DateTime.Today.AddHours(3))
            };

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Shuffle);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(6);
            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Seed.Should().BeGreaterThan(0);

            int firstSeedValue = result.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            Playout result2 = await builder.BuildPlayoutItems(playout, start2, finish2);

            int secondSeedValue = result2.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            firstSeedValue.Should().Be(secondSeedValue);
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_One()
        {
            var floodCollection = new SimpleMediaCollection
            {
                Id = 1,
                Name = "Flood Items",
                Items = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var fixedCollection = new SimpleMediaCollection
            {
                Id = 2,
                Name = "Fixed Items",
                Items = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (floodCollection.Id, floodCollection.Items.ToList()),
                    (fixedCollection.Id, fixedCollection.Items.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Index = 1,
                    MediaCollection = floodCollection,
                    MediaCollectionId = floodCollection.Id,
                    StartTime = null
                },
                new ProgramScheduleItemOne
                {
                    Index = 2,
                    MediaCollection = fixedCollection,
                    MediaCollectionId = fixedCollection.Id,
                    StartTime = TimeSpan.FromHours(3)
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items,
                    MediaCollectionPlaybackOrder = PlaybackOrder.Chronological
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
            };

            var builder = new PlayoutBuilder(fakeRepository, _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(5);
            result.Items[0].Start.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(1);
            result.Items[3].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[3].MediaItemId.Should().Be(3);
            result.Items[4].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(5));
            result.Items[4].MediaItemId.Should().Be(2);
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_Multiple()
        {
            var floodCollection = new SimpleMediaCollection
            {
                Id = 1,
                Name = "Flood Items",
                Items = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var fixedCollection = new SimpleMediaCollection
            {
                Id = 2,
                Name = "Fixed Items",
                Items = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 2))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (floodCollection.Id, floodCollection.Items.ToList()),
                    (fixedCollection.Id, fixedCollection.Items.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Index = 1,
                    MediaCollection = floodCollection,
                    MediaCollectionId = floodCollection.Id,
                    StartTime = null
                },
                new ProgramScheduleItemMultiple
                {
                    Index = 2,
                    MediaCollection = fixedCollection,
                    MediaCollectionId = fixedCollection.Id,
                    StartTime = TimeSpan.FromHours(3),
                    Count = 2
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items,
                    MediaCollectionPlaybackOrder = PlaybackOrder.Chronological
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
            };

            var builder = new PlayoutBuilder(fakeRepository, _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(7);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(6);

            result.Items[0].Start.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(1);

            result.Items[3].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[3].MediaItemId.Should().Be(3);
            result.Items[4].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(5));
            result.Items[4].MediaItemId.Should().Be(4);

            result.Items[5].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[5].MediaItemId.Should().Be(2);
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_DurationWithoutOfflineTail()
        {
            var floodCollection = new SimpleMediaCollection
            {
                Id = 1,
                Name = "Flood Items",
                Items = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var fixedCollection = new SimpleMediaCollection
            {
                Id = 2,
                Name = "Fixed Items",
                Items = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(0.75), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1.5), new DateTime(2020, 1, 2))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (floodCollection.Id, floodCollection.Items.ToList()),
                    (fixedCollection.Id, fixedCollection.Items.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Index = 1,
                    MediaCollection = floodCollection,
                    MediaCollectionId = floodCollection.Id,
                    StartTime = null
                },
                new ProgramScheduleItemDuration
                {
                    Index = 2,
                    MediaCollection = fixedCollection,
                    MediaCollectionId = fixedCollection.Id,
                    StartTime = TimeSpan.FromHours(2),
                    PlayoutDuration = TimeSpan.FromHours(2),
                    OfflineTail = false // immediately continue
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items,
                    MediaCollectionPlaybackOrder = PlaybackOrder.Chronological
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
            };

            var builder = new PlayoutBuilder(fakeRepository, _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(7);

            result.Items[0].Start.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);

            result.Items[2].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(3);

            result.Items[3].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(2.75));
            result.Items[3].MediaItemId.Should().Be(1);
            result.Items[4].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(3.75));
            result.Items[4].MediaItemId.Should().Be(2);

            result.Items[5].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(4.75));
            result.Items[5].MediaItemId.Should().Be(1);
            result.Items[6].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(5.75));
            result.Items[6].MediaItemId.Should().Be(2);
        }

        [Test]
        public async Task MultipleContent_Should_WrapAroundDynamicContent_DurationWithoutOfflineTail()
        {
            var multipleCollection = new SimpleMediaCollection
            {
                Id = 1,
                Name = "Multiple Items",
                Items = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var dynamicCollection = new SimpleMediaCollection
            {
                Id = 2,
                Name = "Dynamic Items",
                Items = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(0.75), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1.5), new DateTime(2020, 1, 2))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (multipleCollection.Id, multipleCollection.Items.ToList()),
                    (dynamicCollection.Id, dynamicCollection.Items.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemMultiple
                {
                    Index = 1,
                    MediaCollection = multipleCollection,
                    MediaCollectionId = multipleCollection.Id,
                    StartTime = null,
                    Count = 2
                },
                new ProgramScheduleItemDuration
                {
                    Index = 2,
                    MediaCollection = dynamicCollection,
                    MediaCollectionId = dynamicCollection.Id,
                    StartTime = null,
                    PlayoutDuration = TimeSpan.FromHours(2),
                    OfflineTail = false // immediately continue
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items,
                    MediaCollectionPlaybackOrder = PlaybackOrder.Chronological
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
            };

            var builder = new PlayoutBuilder(fakeRepository, _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.BuildPlayoutItems(playout, start, finish);

            result.Items.Count.Should().Be(6);

            result.Items[0].Start.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);

            result.Items[2].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(3);

            result.Items[3].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(2.75));
            result.Items[3].MediaItemId.Should().Be(1);
            result.Items[4].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(3.75));
            result.Items[4].MediaItemId.Should().Be(2);

            result.Items[5].Start.TimeOfDay.Should().Be(TimeSpan.FromHours(4.75));
            result.Items[5].MediaItemId.Should().Be(4);
        }

        private static DateTimeOffset HoursAfterMidnight(int hours)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            return now - now.TimeOfDay + TimeSpan.FromHours(hours);
        }

        private static ProgramScheduleItem Flood(MediaCollection mediaCollection) =>
            new ProgramScheduleItemFlood
            {
                Index = 1,
                MediaCollection = mediaCollection,
                MediaCollectionId = mediaCollection.Id,
                StartTime = null
            };

        private static MediaItem TestMovie(int id, TimeSpan duration, DateTime aired) =>
            new()
            {
                Id = id,
                Metadata = new MediaMetadata { Duration = duration, MediaType = MediaType.Movie, Aired = aired }
            };

        private TestData TestDataFloodForItems(List<MediaItem> mediaItems, PlaybackOrder playbackOrder)
        {
            var mediaCollection = new SimpleMediaCollection
            {
                Id = 1,
                Items = mediaItems
            };

            var collectionRepo = new FakeMediaCollectionRepository(Map((mediaCollection.Id, mediaItems)));
            var builder = new PlayoutBuilder(collectionRepo, _logger);

            var items = new List<ProgramScheduleItem> { Flood(mediaCollection) };

            var playout = new Playout
            {
                Id = 1,
                ProgramSchedule = new ProgramSchedule { Items = items, MediaCollectionPlaybackOrder = playbackOrder },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
            };

            return new TestData(builder, playout);
        }

        private record TestData(PlayoutBuilder Builder, Playout Playout);
    }
}
