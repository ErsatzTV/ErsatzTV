using Destructurama;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class PlayoutBuilderTests
{
    private readonly ILogger<PlayoutBuilder> _logger;

    public PlayoutBuilderTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .Destructure.UsingAttributes()
            .CreateLogger();

        ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

        _logger = loggerFactory?.CreateLogger<PlayoutBuilder>();
    }

    [TestFixture]
    public class NewPlayout : PlayoutBuilderTests
    {
        [Test]
        [Timeout(2000)]
        public async Task OnlyZeroDurationItem_Should_Abort()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.Zero, DateTime.Today)
            };

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Random);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset);

            result.Items.Should().BeEmpty();
        }

        [Test]
        public async Task ZeroDurationItem_Should_BeSkipped()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.Zero, DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today)
            };

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Random);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().MediaItemId.Should().Be(2);
            result.Items.Head().StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items.Head().FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));
        }

        [Test]
        [Timeout(2000)]
        public async Task OnlyFileNotFoundItem_Should_Abort()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), DateTime.Today)
            };

            mediaItems[0].State = MediaItemState.FileNotFound;

            var configRepo = new Mock<IConfigElementRepository>();
            configRepo.Setup(
                    repo => repo.GetValue<bool>(
                        It.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key)))
                .ReturnsAsync(Some(true));

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset);

            configRepo.Verify();

            result.Items.Should().BeEmpty();
        }

        [Test]
        public async Task FileNotFoundItem_Should_BeSkipped()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today)
            };

            mediaItems[0].State = MediaItemState.FileNotFound;

            var configRepo = new Mock<IConfigElementRepository>();
            configRepo.Setup(
                    repo => repo.GetValue<bool>(
                        It.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key)))
                .ReturnsAsync(Some(true));

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            configRepo.Verify();

            result.Items.Count.Should().Be(1);
            result.Items.Head().MediaItemId.Should().Be(2);
            result.Items.Head().StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items.Head().FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));
        }

        [Test]
        [Timeout(2000)]
        public async Task OnlyUnavailableItem_Should_Abort()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), DateTime.Today)
            };

            mediaItems[0].State = MediaItemState.Unavailable;

            var configRepo = new Mock<IConfigElementRepository>();
            configRepo.Setup(
                    repo => repo.GetValue<bool>(
                        It.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key)))
                .ReturnsAsync(Some(true));

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset);

            configRepo.Verify();

            result.Items.Should().BeEmpty();
        }

        [Test]
        public async Task UnavailableItem_Should_BeSkipped()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today)
            };

            mediaItems[0].State = MediaItemState.Unavailable;

            var configRepo = new Mock<IConfigElementRepository>();
            configRepo.Setup(
                    repo => repo.GetValue<bool>(
                        It.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key)))
                .ReturnsAsync(Some(true));

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            configRepo.Verify();

            result.Items.Count.Should().Be(1);
            result.Items.Head().MediaItemId.Should().Be(2);
            result.Items.Head().StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items.Head().FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));
        }

        [Test]
        public async Task FileNotFound_Should_NotBeSkippedIfConfigured()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today)
            };

            mediaItems[0].State = MediaItemState.FileNotFound;

            var configRepo = new Mock<IConfigElementRepository>();
            configRepo.Setup(
                    repo => repo.GetValue<bool>(
                        It.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key)))
                .ReturnsAsync(Some(false));

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items.Head().FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));
        }

        [Test]
        public async Task Unavailable_Should_NotBeSkippedIfConfigured()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today)
            };

            mediaItems[0].State = MediaItemState.Unavailable;

            var configRepo = new Mock<IConfigElementRepository>();
            configRepo.Setup(
                    repo => repo.GetValue<bool>(
                        It.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key)))
                .ReturnsAsync(Some(false));

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items.Head().FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));
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

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items.Head().FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));
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

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(2);
            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[1].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(12));
        }

        [Test]
        public async Task ChronologicalContent_Should_CreateChronologicalItems()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
            };

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(4);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(4);
            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(1);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[3].MediaItemId.Should().Be(2);

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(4));
        }

        [Test]
        public async Task ChronologicalFlood_Should_AnchorAndMaintainExistingPlayout()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today.AddHours(1))
            };

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().MediaItemId.Should().Be(1);

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));

            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);

            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            Playout result2 = await builder.Build(playout, PlayoutBuildMode.Continue, start2, finish2);

            result2.Items.Count.Should().Be(2);
            result2.Items.Last().StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result2.Items.Last().MediaItemId.Should().Be(2);

            result2.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(12));
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

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().MediaItemId.Should().Be(1);

            result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));
            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);

            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(12);

            Playout result2 = await builder.Build(playout, PlayoutBuildMode.Continue, start2, finish2);

            result2.Items.Count.Should().Be(3);
            result2.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result2.Items[1].MediaItemId.Should().Be(2);
            result2.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result2.Items[2].MediaItemId.Should().Be(1);

            result2.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(18));
            result2.ProgramScheduleAnchors.Count.Should().Be(1);
            result2.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);
        }

        [Test]
        public async Task ShuffleFloodReset_Should_IgnoreAnchors()
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

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(6);
            result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));

            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);

            int firstSeedValue = result.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            Playout result2 = await builder.Build(playout, PlayoutBuildMode.Reset, start2, finish2);

            result2.Items.Count.Should().Be(6);
            result2.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));

            result2.ProgramScheduleAnchors.Count.Should().Be(1);
            result2.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);

            int secondSeedValue = result2.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            firstSeedValue.Should().NotBe(secondSeedValue);
        }

        [Test]
        public async Task ContinuePlayout_ShuffleFlood_Should_MaintainRandomSeed()
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

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(6);
            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Seed.Should().BeGreaterThan(0);

            int firstSeedValue = result.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            Playout result2 = await builder.Build(playout, PlayoutBuildMode.Continue, start2, finish2);

            int secondSeedValue = result2.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            firstSeedValue.Should().Be(secondSeedValue);

            result2.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_One()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (floodCollection.Id, floodCollection.MediaItems.ToList()),
                    (fixedCollection.Id, fixedCollection.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Id = 1,
                    Index = 1,
                    Collection = floodCollection,
                    CollectionId = floodCollection.Id,
                    StartTime = null,
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemOne
                {
                    Id = 2,
                    Index = 2,
                    Collection = fixedCollection,
                    CollectionId = fixedCollection.Id,
                    StartTime = TimeSpan.FromHours(3),
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(5);
            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(1);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[3].MediaItemId.Should().Be(3);
            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(5));
            result.Items[4].MediaItemId.Should().Be(2);

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_One_Multiple_Days()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (floodCollection.Id, floodCollection.MediaItems.ToList()),
                    (fixedCollection.Id, fixedCollection.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Id = 1,
                    Index = 1,
                    Collection = floodCollection,
                    CollectionId = floodCollection.Id,
                    StartTime = null,
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemOne
                {
                    Id = 2,
                    Index = 2,
                    Collection = fixedCollection,
                    CollectionId = fixedCollection.Id,
                    StartTime = TimeSpan.FromHours(3),
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(30);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(28);
            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(1);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[3].MediaItemId.Should().Be(3);
            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(5));
            result.Items[4].MediaItemId.Should().Be(2);
            result.Items[5].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[5].MediaItemId.Should().Be(1);
            result.Items[6].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(7));
            result.Items[6].MediaItemId.Should().Be(2);
            result.Items[7].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(8));
            result.Items[7].MediaItemId.Should().Be(1);
            result.Items[8].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(9));
            result.Items[8].MediaItemId.Should().Be(2);
            result.Items[9].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(10));
            result.Items[9].MediaItemId.Should().Be(1);
            result.Items[10].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(11));
            result.Items[10].MediaItemId.Should().Be(2);
            result.Items[11].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result.Items[11].MediaItemId.Should().Be(1);
            result.Items[12].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(13));
            result.Items[12].MediaItemId.Should().Be(2);
            result.Items[13].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(14));
            result.Items[13].MediaItemId.Should().Be(1);
            result.Items[14].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(15));
            result.Items[14].MediaItemId.Should().Be(2);
            result.Items[15].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(16));
            result.Items[15].MediaItemId.Should().Be(1);
            result.Items[16].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(17));
            result.Items[16].MediaItemId.Should().Be(2);
            result.Items[17].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(18));
            result.Items[17].MediaItemId.Should().Be(1);
            result.Items[18].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(19));
            result.Items[18].MediaItemId.Should().Be(2);
            result.Items[19].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(20));
            result.Items[19].MediaItemId.Should().Be(1);
            result.Items[20].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(21));
            result.Items[20].MediaItemId.Should().Be(2);
            result.Items[21].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(22));
            result.Items[21].MediaItemId.Should().Be(1);
            result.Items[22].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(23));
            result.Items[22].MediaItemId.Should().Be(2);
            result.Items[23].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[23].MediaItemId.Should().Be(1);
            result.Items[24].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[24].MediaItemId.Should().Be(2);
            result.Items[25].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[25].MediaItemId.Should().Be(1);
            result.Items[26].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[26].MediaItemId.Should().Be(3);
            result.Items[27].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(5));
            result.Items[27].MediaItemId.Should().Be(2);

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(30));
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_Multiple()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 2))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (floodCollection.Id, floodCollection.MediaItems.ToList()),
                    (fixedCollection.Id, fixedCollection.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Id = 1,
                    Index = 1,
                    Collection = floodCollection,
                    CollectionId = floodCollection.Id,
                    StartTime = null,
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemMultiple
                {
                    Id = 2,
                    Index = 2,
                    Collection = fixedCollection,
                    CollectionId = fixedCollection.Id,
                    StartTime = TimeSpan.FromHours(3),
                    Count = 2,
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(7);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(6);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(1);

            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[3].MediaItemId.Should().Be(3);
            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(5));
            result.Items[4].MediaItemId.Should().Be(4);

            result.Items[5].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[5].MediaItemId.Should().Be(2);

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(7));
        }

        [Test]
        public async Task FloodContent_Should_FloodWithFixedStartTime()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 2))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (floodCollection.Id, floodCollection.MediaItems.ToList()),
                    (fixedCollection.Id, fixedCollection.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Id = 1,
                    Index = 1,
                    Collection = floodCollection,
                    CollectionId = floodCollection.Id,
                    StartTime = TimeSpan.FromHours(7),
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemOne
                {
                    Id = 2,
                    Index = 2,
                    Collection = fixedCollection,
                    CollectionId = fixedCollection.Id,
                    StartTime = TimeSpan.FromHours(12),
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(24);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(6);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(7));
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(8));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(9));
            result.Items[2].MediaItemId.Should().Be(1);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(10));
            result.Items[3].MediaItemId.Should().Be(2);
            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(11));
            result.Items[4].MediaItemId.Should().Be(1);

            result.Items[5].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result.Items[5].MediaItemId.Should().Be(3);

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(31));
        }

        [Test]
        public async Task ContinuePlayout_FloodContent_Should_FloodWithFixedStartTime_FromAnchor()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 2))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (floodCollection.Id, floodCollection.MediaItems.ToList()),
                    (fixedCollection.Id, fixedCollection.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Id = 1,
                    Index = 1,
                    Collection = floodCollection,
                    CollectionId = floodCollection.Id,
                    StartTime = TimeSpan.FromHours(7),
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemOne
                {
                    Id = 2,
                    Index = 2,
                    Collection = fixedCollection,
                    CollectionId = fixedCollection.Id,
                    StartTime = TimeSpan.FromHours(12),
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                Anchor = new PlayoutAnchor
                {
                    NextStart = HoursAfterMidnight(9).UtcDateTime,
                    ScheduleItemsEnumeratorState = new CollectionEnumeratorState
                    {
                        Index = 0,
                        Seed = 1
                    },
                    InFlood = true
                },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(32);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Continue, start, finish);

            result.Items.Count.Should().Be(5);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(9));
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(10));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(11));
            result.Items[2].MediaItemId.Should().Be(1);

            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result.Items[3].MediaItemId.Should().Be(3);

            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(7));
            result.Items[4].MediaItemId.Should().Be(2);

            result.Anchor.InFlood.Should().BeTrue();

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(32));
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_DurationWithoutOfflineTail()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(0.75), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1.5), new DateTime(2020, 1, 2))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (floodCollection.Id, floodCollection.MediaItems.ToList()),
                    (fixedCollection.Id, fixedCollection.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Id = 1,
                    Index = 1,
                    Collection = floodCollection,
                    CollectionId = floodCollection.Id,
                    StartTime = null,
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemDuration
                {
                    Id = 2,
                    Index = 2,
                    Collection = fixedCollection,
                    CollectionId = fixedCollection.Id,
                    StartTime = TimeSpan.FromHours(2),
                    PlayoutDuration = TimeSpan.FromHours(2),
                    TailMode = TailMode.None, // immediately continue
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(7);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);

            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(3);

            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2.75));
            result.Items[3].MediaItemId.Should().Be(1);
            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3.75));
            result.Items[4].MediaItemId.Should().Be(2);

            result.Items[5].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(4.75));
            result.Items[5].MediaItemId.Should().Be(1);
            result.Items[6].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(5.75));
            result.Items[6].MediaItemId.Should().Be(2);

            result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6.75));
        }

        [Test]
        public async Task MultipleContent_Should_WrapAroundDynamicContent_DurationWithoutOfflineTail()
        {
            var multipleCollection = new Collection
            {
                Id = 1,
                Name = "Multiple Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var dynamicCollection = new Collection
            {
                Id = 2,
                Name = "Dynamic Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(0.75), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1.5), new DateTime(2020, 1, 2))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (multipleCollection.Id, multipleCollection.MediaItems.ToList()),
                    (dynamicCollection.Id, dynamicCollection.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemMultiple
                {
                    Id = 1,
                    Index = 1,
                    Collection = multipleCollection,
                    CollectionId = multipleCollection.Id,
                    StartTime = null,
                    Count = 2,
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemDuration
                {
                    Id = 2,
                    Index = 2,
                    Collection = dynamicCollection,
                    CollectionId = dynamicCollection.Id,
                    StartTime = null,
                    PlayoutDuration = TimeSpan.FromHours(2),
                    TailMode = TailMode.None, // immediately continue
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(6);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);

            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(3);

            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2.75));
            result.Items[3].MediaItemId.Should().Be(1);
            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3.75));
            result.Items[4].MediaItemId.Should().Be(2);

            result.Items[5].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(4.75));
            result.Items[5].MediaItemId.Should().Be(4);

            result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6.25));
        }

        [Test]
        public async Task ContinuePlayout_Alternating_MultipleContent_Should_Maintain_Counts()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Multiple Items 1",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Multiple Items 2",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (collectionOne.Id, collectionOne.MediaItems.ToList()),
                    (collectionTwo.Id, collectionTwo.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemMultiple
                {
                    Id = 1,
                    Index = 1,
                    Collection = collectionOne,
                    CollectionId = collectionOne.Id,
                    StartTime = null,
                    Count = 3,
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemMultiple
                {
                    Id = 2,
                    Index = 2,
                    Collection = collectionTwo,
                    CollectionId = collectionTwo.Id,
                    StartTime = null,
                    Count = 3,
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                Anchor = new PlayoutAnchor
                {
                    NextStart = HoursAfterMidnight(1).UtcDateTime,
                    ScheduleItemsEnumeratorState = new CollectionEnumeratorState
                    {
                        Index = 0,
                        Seed = 1
                    },
                    MultipleRemaining = 2
                },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(5);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Continue, start, finish);

            result.Items.Count.Should().Be(4);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[1].MediaItemId.Should().Be(1);

            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[2].MediaItemId.Should().Be(2);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(4));
            result.Items[3].MediaItemId.Should().Be(2);

            result.Anchor.ScheduleItemsEnumeratorState.Index.Should().Be(1);
            result.Anchor.MultipleRemaining.Should().Be(1);
            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(5));
        }

        [Test]
        public async Task Auto_Zero_MultipleCount()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Multiple Items 1",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(3, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Multiple Items 2",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(5, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (collectionOne.Id, collectionOne.MediaItems.ToList()),
                    (collectionTwo.Id, collectionTwo.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemMultiple
                {
                    Id = 1,
                    Index = 1,
                    Collection = collectionOne,
                    CollectionId = collectionOne.Id,
                    StartTime = null,
                    Count = 0,
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemMultiple
                {
                    Id = 2,
                    Index = 2,
                    Collection = collectionTwo,
                    CollectionId = collectionTwo.Id,
                    StartTime = null,
                    Count = 0,
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(5);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(5);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(0));
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(3);

            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[3].MediaItemId.Should().Be(4);
            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(4));
            result.Items[4].MediaItemId.Should().Be(5);

            result.Anchor.ScheduleItemsEnumeratorState.Index.Should().Be(0);
            result.Anchor.MultipleRemaining.Should().BeNull();
            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(5));
        }

        [Test]
        public async Task ContinuePlayout_Alternating_Duration_Should_Maintain_Duration()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Duration Items 2",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (collectionOne.Id, collectionOne.MediaItems.ToList()),
                    (collectionTwo.Id, collectionTwo.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemDuration
                {
                    Id = 1,
                    Index = 1,
                    Collection = collectionOne,
                    CollectionId = collectionOne.Id,
                    StartTime = null,
                    PlayoutDuration = TimeSpan.FromHours(3),
                    TailMode = TailMode.None,
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemDuration
                {
                    Id = 2,
                    Index = 2,
                    Collection = collectionTwo,
                    CollectionId = collectionTwo.Id,
                    StartTime = null,
                    PlayoutDuration = TimeSpan.FromHours(3),
                    TailMode = TailMode.None,
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                Anchor = new PlayoutAnchor
                {
                    NextStart = HoursAfterMidnight(1).UtcDateTime,
                    ScheduleItemsEnumeratorState = new CollectionEnumeratorState
                    {
                        Index = 0,
                        Seed = 1
                    },
                    DurationFinish = HoursAfterMidnight(3).UtcDateTime
                },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(5);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Continue, start, finish);

            result.Items.Count.Should().Be(4);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[1].MediaItemId.Should().Be(1);

            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[2].MediaItemId.Should().Be(2);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(4));
            result.Items[3].MediaItemId.Should().Be(2);

            result.Anchor.ScheduleItemsEnumeratorState.Index.Should().Be(1);
            result.Anchor.DurationFinish.Should().Be(HoursAfterMidnight(6).UtcDateTime);
            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(5));
        }

        [Test]
        public async Task Alternating_Duration_With_Filler_Should_Alternate_Schedule_Items()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromMinutes(55), new DateTime(2020, 1, 1))
                }
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Duration Items 2",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(2, TimeSpan.FromMinutes(55), new DateTime(2020, 1, 1))
                }
            };

            var collectionThree = new Collection
            {
                Id = 3,
                Name = "Filler Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromMinutes(5), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (collectionOne.Id, collectionOne.MediaItems.ToList()),
                    (collectionTwo.Id, collectionTwo.MediaItems.ToList()),
                    (collectionThree.Id, collectionThree.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemDuration
                {
                    Id = 1,
                    Index = 1,
                    Collection = collectionOne,
                    CollectionId = collectionOne.Id,
                    StartTime = null,
                    PlayoutDuration = TimeSpan.FromHours(3),
                    PlaybackOrder = PlaybackOrder.Chronological,
                    TailMode = TailMode.Filler,
                    TailFiller = new FillerPreset
                    {
                        FillerKind = FillerKind.Tail,
                        Collection = collectionThree,
                        CollectionId = collectionThree.Id
                    }
                },
                new ProgramScheduleItemDuration
                {
                    Id = 2,
                    Index = 2,
                    Collection = collectionTwo,
                    CollectionId = collectionTwo.Id,
                    StartTime = null,
                    PlayoutDuration = TimeSpan.FromHours(3),
                    PlaybackOrder = PlaybackOrder.Chronological,
                    TailMode = TailMode.Filler,
                    TailFiller = new FillerPreset
                    {
                        FillerKind = FillerKind.Tail,
                        Collection = collectionThree,
                        CollectionId = collectionThree.Id
                    }
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(12);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromMinutes(0));
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromMinutes(55));
            result.Items[1].MediaItemId.Should().Be(1);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(new TimeSpan(1, 50, 0));
            result.Items[2].MediaItemId.Should().Be(1);

            result.Items[3].StartOffset.TimeOfDay.Should().Be(new TimeSpan(2, 45, 0));
            result.Items[3].MediaItemId.Should().Be(3);
            result.Items[4].StartOffset.TimeOfDay.Should().Be(new TimeSpan(2, 50, 0));
            result.Items[4].MediaItemId.Should().Be(3);
            result.Items[5].StartOffset.TimeOfDay.Should().Be(new TimeSpan(2, 55, 0));
            result.Items[5].MediaItemId.Should().Be(3);

            result.Items[6].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[6].MediaItemId.Should().Be(2);
            result.Items[7].StartOffset.TimeOfDay.Should().Be(new TimeSpan(3, 55, 0));
            result.Items[7].MediaItemId.Should().Be(2);
            result.Items[8].StartOffset.TimeOfDay.Should().Be(new TimeSpan(4, 50, 0));
            result.Items[8].MediaItemId.Should().Be(2);

            result.Items[9].StartOffset.TimeOfDay.Should().Be(new TimeSpan(5, 45, 0));
            result.Items[9].MediaItemId.Should().Be(3);
            result.Items[10].StartOffset.TimeOfDay.Should().Be(new TimeSpan(5, 50, 0));
            result.Items[10].MediaItemId.Should().Be(3);
            result.Items[11].StartOffset.TimeOfDay.Should().Be(new TimeSpan(5, 55, 0));
            result.Items[11].MediaItemId.Should().Be(3);

            result.Anchor.ScheduleItemsEnumeratorState.Index.Should().Be(0);
            result.Anchor.DurationFinish.Should().BeNull();

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));
        }

        [Test]
        public async Task Multiple_With_Filler_Should_Keep_Filler_After_End_Of_Playout()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromMinutes(61), new DateTime(2020, 1, 1))
                }
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Filler Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(2, TimeSpan.FromMinutes(4), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (collectionOne.Id, collectionOne.MediaItems.ToList()),
                    (collectionTwo.Id, collectionTwo.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemMultiple
                {
                    Id = 1,
                    Index = 1,
                    Collection = collectionOne,
                    CollectionId = collectionOne.Id,
                    StartTime = null,
                    Count = 1,
                    PlaybackOrder = PlaybackOrder.Chronological,
                    PostRollFiller = new FillerPreset
                    {
                        FillerKind = FillerKind.PostRoll,
                        Collection = collectionTwo,
                        CollectionId = collectionTwo.Id,
                        FillerMode = FillerMode.Count,
                        Count = 1
                    }
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(1);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(2);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromMinutes(0));
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromMinutes(61));
            result.Items[1].MediaItemId.Should().Be(2);

            result.Anchor.ScheduleItemsEnumeratorState.Index.Should().Be(0);
            result.Anchor.DurationFinish.Should().BeNull();
            result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddMinutes(65));
        }

        [Test]
        public async Task Duration_Should_Skip_Items_That_Are_Too_Long()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository =
                new FakeMediaCollectionRepository(Map((collectionOne.Id, collectionOne.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemDuration
                {
                    Id = 1,
                    Index = 1,
                    Collection = collectionOne,
                    CollectionId = collectionOne.Id,
                    StartTime = null,
                    PlayoutDuration = TimeSpan.FromHours(1),
                    PlaybackOrder = PlaybackOrder.Chronological,
                    TailMode = TailMode.None
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(6);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(0));
            result.Items[0].MediaItemId.Should().Be(2);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[1].MediaItemId.Should().Be(4);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[2].MediaItemId.Should().Be(2);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[3].MediaItemId.Should().Be(4);
            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(4));
            result.Items[4].MediaItemId.Should().Be(2);
            result.Items[5].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(5));
            result.Items[5].MediaItemId.Should().Be(4);

            result.Anchor.ScheduleItemsEnumeratorState.Index.Should().Be(0);
            result.Anchor.DurationFinish.Should().BeNull();
            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(6));
        }

        [Test]
        public async Task Two_Day_Playout_Should_Create_Date_Anchors_For_Midnight()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), new DateTime(2002, 1, 1)),
                TestMovie(2, TimeSpan.FromHours(6), new DateTime(2003, 1, 1)),
                TestMovie(3, TimeSpan.FromHours(6), new DateTime(2004, 1, 1))
            };

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromDays(2);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(8);
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[1].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result.Items[2].MediaItemId.Should().Be(3);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result.Items[2].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(18));
            result.Items[3].MediaItemId.Should().Be(1);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(18));
            result.Items[3].FinishOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[4].MediaItemId.Should().Be(2);
            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[4].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[5].MediaItemId.Should().Be(3);
            result.Items[5].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[5].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result.Items[6].MediaItemId.Should().Be(1);
            result.Items[6].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result.Items[6].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(18));
            result.Items[7].MediaItemId.Should().Be(2);
            result.Items[7].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(18));
            result.Items[7].FinishOffset.TimeOfDay.Should().Be(TimeSpan.Zero);

            result.ProgramScheduleAnchors.Count.Should().Be(2);
            result.ProgramScheduleAnchors.Count(a => a.EnumeratorState.Index == 4 % 3).Should().Be(1);
            result.ProgramScheduleAnchors.Count(a => a.EnumeratorState.Index == 8 % 3).Should().Be(1);

            int seed = result.ProgramScheduleAnchors[0].EnumeratorState.Seed;
            result.ProgramScheduleAnchors.All(a => a.EnumeratorState.Seed == seed).Should().BeTrue();

            result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddDays(2));
        }
    }

    [TestFixture]
    public class ResetPlayout : PlayoutBuilderTests
    {
        [Test]
        public async Task ShuffleFlood_Should_IgnoreAnchors()
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

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(6);
            result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));

            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);

            int firstSeedValue = result.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            Playout result2 = await builder.Build(playout, PlayoutBuildMode.Reset, start2, finish2);

            result2.Items.Count.Should().Be(6);
            result2.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));

            result2.ProgramScheduleAnchors.Count.Should().Be(1);
            result2.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);

            int secondSeedValue = result2.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            firstSeedValue.Should().NotBe(secondSeedValue);
        }
    }

    [TestFixture]
    public class RefreshPlayout : PlayoutBuilderTests
    {
        [Test]
        public async Task Two_Day_Playout_Should_Refresh_From_Midnight_Anchor()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(6), new DateTime(2002, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(6), new DateTime(2003, 1, 1)),
                    TestMovie(3, TimeSpan.FromHours(6), new DateTime(2004, 1, 1))
                }
            };

            var fakeRepository =
                new FakeMediaCollectionRepository(Map((collectionOne.Id, collectionOne.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Id = 1,
                    Index = 1,
                    Collection = collectionOne,
                    CollectionId = collectionOne.Id,
                    StartTime = null,
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },

                // this should be ignored
                Anchor = new PlayoutAnchor
                {
                    NextStart = HoursAfterMidnight(1).UtcDateTime,
                    ScheduleItemsEnumeratorState = new CollectionEnumeratorState
                    {
                        Index = 0,
                        Seed = 1
                    },
                    DurationFinish = HoursAfterMidnight(3).UtcDateTime
                },

                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            playout.ProgramScheduleAnchors.Add(
                new PlayoutProgramScheduleAnchor
                {
                    AnchorDate = HoursAfterMidnight(24).UtcDateTime,
                    Collection = collectionOne,
                    CollectionId = collectionOne.Id,
                    CollectionType = ProgramScheduleItemCollectionType.Collection,
                    EnumeratorState = new CollectionEnumeratorState
                    {
                        Index = 1,
                        Seed = 12345
                    },
                    Playout = playout,
                    ProgramSchedule = playout.ProgramSchedule
                });

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(24);
            DateTimeOffset finish = start + TimeSpan.FromDays(1);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Refresh, start, finish);

            result.Items.Count.Should().Be(4);
            result.Items[0].MediaItemId.Should().Be(2);
            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
            result.Items[0].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[1].MediaItemId.Should().Be(3);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result.Items[1].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result.Items[2].MediaItemId.Should().Be(1);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result.Items[2].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(18));
            result.Items[3].MediaItemId.Should().Be(2);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(18));
            result.Items[3].FinishOffset.TimeOfDay.Should().Be(TimeSpan.Zero);

            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(48));
        }
    }

    [TestFixture]
    public class ContinuePlayout : PlayoutBuilderTests
    {
        [Test]
        public async Task ChronologicalFlood_Should_AnchorAndMaintainExistingPlayout()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today.AddHours(1))
            };

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().MediaItemId.Should().Be(1);

            result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));

            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);

            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            Playout result2 = await builder.Build(playout, PlayoutBuildMode.Continue, start2, finish2);

            result2.Items.Count.Should().Be(2);
            result2.Items.Last().StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result2.Items.Last().MediaItemId.Should().Be(2);

            result2.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(12));
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

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(1);
            result.Items.Head().MediaItemId.Should().Be(1);

            result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));
            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);

            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(12);

            Playout result2 = await builder.Build(playout, PlayoutBuildMode.Continue, start2, finish2);

            result2.Items.Count.Should().Be(3);
            result2.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            result2.Items[1].MediaItemId.Should().Be(2);
            result2.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result2.Items[2].MediaItemId.Should().Be(1);

            result2.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(18));
            result2.ProgramScheduleAnchors.Count.Should().Be(1);
            result2.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);
        }

        [Test]
        public async Task ChronologicalFlood_Should_AnchorAndReturnNewPlayoutItems_MultiDay()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today.AddHours(1))
            };

            (PlayoutBuilder builder, Playout playout) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromDays(1);

            await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            playout.Items.Count.Should().Be(4);
            playout.Items.Map(i => i.MediaItemId).ToList().Should().Equal(1, 2, 1, 2);

            playout.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddDays(1));
            playout.ProgramScheduleAnchors.Count.Should().Be(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);

            PlayoutProgramScheduleAnchor headAnchor = playout.ProgramScheduleAnchors.Head();

            // throw in a detractor anchor - playout builder should prioritize the "continue" anchor
            playout.ProgramScheduleAnchors.Insert(
                0,
                new PlayoutProgramScheduleAnchor
                {
                    Id = headAnchor.Id + 1,
                    Collection = headAnchor.Collection,
                    CollectionId = headAnchor.CollectionId,
                    Playout = playout,
                    PlayoutId = playout.Id,
                    AnchorDate = DateTime.Today.ToUniversalTime(),
                    CollectionType = headAnchor.CollectionType,
                    EnumeratorState = new CollectionEnumeratorState
                        { Index = headAnchor.EnumeratorState.Index + 1, Seed = headAnchor.EnumeratorState.Seed },
                    MediaItem = headAnchor.MediaItem,
                    MediaItemId = headAnchor.MediaItemId,
                    MultiCollection = headAnchor.MultiCollection,
                    MultiCollectionId = headAnchor.MultiCollectionId,
                    ProgramSchedule = headAnchor.ProgramSchedule,
                    ProgramScheduleId = headAnchor.ProgramScheduleId,
                    SmartCollection = headAnchor.SmartCollection,
                    SmartCollectionId = headAnchor.SmartCollectionId
                });

            // continue 1h later
            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromDays(1);

            await builder.Build(playout, PlayoutBuildMode.Continue, start2, finish2);

            playout.Items.Count.Should().Be(5);
            playout.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(0));
            playout.Items[0].MediaItemId.Should().Be(1);
            playout.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            playout.Items[1].MediaItemId.Should().Be(2);
            playout.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            playout.Items[2].MediaItemId.Should().Be(1);
            playout.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(18));
            playout.Items[3].MediaItemId.Should().Be(2);
            playout.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(0));
            playout.Items[4].MediaItemId.Should().Be(1);

            playout.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(30));

            playout.ProgramScheduleAnchors.Count.Should().Be(2);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);

            // continue 1h later
            DateTimeOffset start3 = HoursAfterMidnight(2);
            DateTimeOffset finish3 = start3 + TimeSpan.FromDays(1);

            await builder.Build(playout, PlayoutBuildMode.Continue, start3, finish3);

            playout.Items.Count.Should().Be(5);
            playout.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(0));
            playout.Items[0].MediaItemId.Should().Be(1);
            playout.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
            playout.Items[1].MediaItemId.Should().Be(2);
            playout.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            playout.Items[2].MediaItemId.Should().Be(1);
            playout.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(18));
            playout.Items[3].MediaItemId.Should().Be(2);
            playout.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(0));
            playout.Items[4].MediaItemId.Should().Be(1);

            playout.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(30));

            playout.ProgramScheduleAnchors.Count.Should().Be(2);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);
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

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(6);
            result.ProgramScheduleAnchors.Count.Should().Be(1);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Seed.Should().BeGreaterThan(0);
            result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);

            int firstSeedValue = result.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            Playout result2 = await builder.Build(result, PlayoutBuildMode.Continue, start2, finish2);

            int secondSeedValue = result2.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            firstSeedValue.Should().Be(secondSeedValue);

            result2.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);
        }

        [Test]
        public async Task ShuffleFlood_Should_MaintainRandomSeed_MultipleDays()
        {
            var mediaItems = new List<MediaItem>();
            for (int i = 1; i <= 25; i++)
            {
                mediaItems.Add(TestMovie(i, TimeSpan.FromMinutes(55), DateTime.Today.AddHours(i)));
            }

            (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Shuffle);
            DateTimeOffset start = HoursAfterMidnight(0).AddSeconds(5);
            DateTimeOffset finish = start + TimeSpan.FromDays(2);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Reset, start, finish);

            result.Items.Count.Should().Be(53);
            result.ProgramScheduleAnchors.Count.Should().Be(2);

            result.ProgramScheduleAnchors.All(x => x.AnchorDate is not null).Should().BeTrue();
            PlayoutProgramScheduleAnchor lastCheckpoint = result.ProgramScheduleAnchors
                .OrderByDescending(a => a.AnchorDate ?? DateTime.MinValue)
                .First();
            lastCheckpoint.EnumeratorState.Seed.Should().BeGreaterThan(0);
            lastCheckpoint.EnumeratorState.Index.Should().Be(3);
            
            // we need to mess up the ordering to trigger the problematic behavior
            // this simulates the way the rows are loaded with EF
            PlayoutProgramScheduleAnchor oldest = result.ProgramScheduleAnchors.OrderByDescending(a => a.AnchorDate).Last();
            PlayoutProgramScheduleAnchor newest = result.ProgramScheduleAnchors.OrderByDescending(a => a.AnchorDate).First();

            result.ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>
            {
                oldest,
                newest
            };

            int firstSeedValue = lastCheckpoint.EnumeratorState.Seed;

            DateTimeOffset start2 = start.AddHours(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromDays(2);

            Playout result2 = await builder.Build(result, PlayoutBuildMode.Continue, start2, finish2);

            PlayoutProgramScheduleAnchor continueAnchor =
                result2.ProgramScheduleAnchors.First(x => x.AnchorDate is null);
            int secondSeedValue = continueAnchor.EnumeratorState.Seed;
            
            // the continue anchor should have the same seed as the most recent (last) checkpoint from the first run
            firstSeedValue.Should().Be(secondSeedValue);
        }

        [Test]
        public async Task FloodContent_Should_FloodWithFixedStartTime_FromAnchor()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                }
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 2))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (floodCollection.Id, floodCollection.MediaItems.ToList()),
                    (fixedCollection.Id, fixedCollection.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemFlood
                {
                    Id = 1,
                    Index = 1,
                    Collection = floodCollection,
                    CollectionId = floodCollection.Id,
                    StartTime = TimeSpan.FromHours(7),
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemOne
                {
                    Id = 2,
                    Index = 2,
                    Collection = fixedCollection,
                    CollectionId = fixedCollection.Id,
                    StartTime = TimeSpan.FromHours(12),
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                Anchor = new PlayoutAnchor
                {
                    NextStart = HoursAfterMidnight(9).UtcDateTime,
                    ScheduleItemsEnumeratorState = new CollectionEnumeratorState
                    {
                        Index = 0,
                        Seed = 1
                    },
                    InFlood = true
                },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(32);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Continue, start, finish);

            result.Items.Count.Should().Be(5);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(9));
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(10));
            result.Items[1].MediaItemId.Should().Be(2);
            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(11));
            result.Items[2].MediaItemId.Should().Be(1);

            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
            result.Items[3].MediaItemId.Should().Be(3);

            result.Items[4].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(7));
            result.Items[4].MediaItemId.Should().Be(2);

            result.Anchor.InFlood.Should().BeTrue();
            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(32));
        }

        [Test]
        public async Task Alternating_MultipleContent_Should_Maintain_Counts()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Multiple Items 1",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Multiple Items 2",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (collectionOne.Id, collectionOne.MediaItems.ToList()),
                    (collectionTwo.Id, collectionTwo.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemMultiple
                {
                    Id = 1,
                    Index = 1,
                    Collection = collectionOne,
                    CollectionId = collectionOne.Id,
                    StartTime = null,
                    Count = 3,
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemMultiple
                {
                    Id = 2,
                    Index = 2,
                    Collection = collectionTwo,
                    CollectionId = collectionTwo.Id,
                    StartTime = null,
                    Count = 3,
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                Anchor = new PlayoutAnchor
                {
                    NextStart = HoursAfterMidnight(1).UtcDateTime,
                    ScheduleItemsEnumeratorState = new CollectionEnumeratorState
                    {
                        Index = 0,
                        Seed = 1
                    },
                    MultipleRemaining = 2
                },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(5);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Continue, start, finish);

            result.Items.Count.Should().Be(4);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[1].MediaItemId.Should().Be(1);

            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[2].MediaItemId.Should().Be(2);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(4));
            result.Items[3].MediaItemId.Should().Be(2);

            result.Anchor.ScheduleItemsEnumeratorState.Index.Should().Be(1);
            result.Anchor.MultipleRemaining.Should().Be(1);
            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(5));
        }

        [Test]
        public async Task Alternating_Duration_Should_Maintain_Duration()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Duration Items 2",
                MediaItems = new List<MediaItem>
                {
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                }
            };

            var fakeRepository = new FakeMediaCollectionRepository(
                Map(
                    (collectionOne.Id, collectionOne.MediaItems.ToList()),
                    (collectionTwo.Id, collectionTwo.MediaItems.ToList())));

            var items = new List<ProgramScheduleItem>
            {
                new ProgramScheduleItemDuration
                {
                    Id = 1,
                    Index = 1,
                    Collection = collectionOne,
                    CollectionId = collectionOne.Id,
                    StartTime = null,
                    PlayoutDuration = TimeSpan.FromHours(3),
                    TailMode = TailMode.None,
                    PlaybackOrder = PlaybackOrder.Chronological
                },
                new ProgramScheduleItemDuration
                {
                    Id = 2,
                    Index = 2,
                    Collection = collectionTwo,
                    CollectionId = collectionTwo.Id,
                    StartTime = null,
                    PlayoutDuration = TimeSpan.FromHours(3),
                    TailMode = TailMode.None,
                    PlaybackOrder = PlaybackOrder.Chronological
                }
            };

            var playout = new Playout
            {
                ProgramSchedule = new ProgramSchedule
                {
                    Items = items
                },
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                Anchor = new PlayoutAnchor
                {
                    NextStart = HoursAfterMidnight(1).UtcDateTime,
                    ScheduleItemsEnumeratorState = new CollectionEnumeratorState
                    {
                        Index = 0,
                        Seed = 1
                    },
                    DurationFinish = HoursAfterMidnight(3).UtcDateTime
                },
                ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>(),
                Items = new List<PlayoutItem>()
            };

            var configRepo = new Mock<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            var artistRepo = new Mock<IArtistRepository>();
            var builder = new PlayoutBuilder(
                configRepo.Object,
                fakeRepository,
                televisionRepo,
                artistRepo.Object,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(5);

            Playout result = await builder.Build(playout, PlayoutBuildMode.Continue, start, finish);

            result.Items.Count.Should().Be(4);

            result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
            result.Items[0].MediaItemId.Should().Be(1);
            result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
            result.Items[1].MediaItemId.Should().Be(1);

            result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
            result.Items[2].MediaItemId.Should().Be(2);
            result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(4));
            result.Items[3].MediaItemId.Should().Be(2);

            result.Anchor.ScheduleItemsEnumeratorState.Index.Should().Be(1);
            result.Anchor.DurationFinish.Should().Be(HoursAfterMidnight(6).UtcDateTime);
            result.Anchor.NextStartOffset.Should().Be(HoursAfterMidnight(5));
        }
    }

    private static DateTimeOffset HoursAfterMidnight(int hours)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        return now - now.TimeOfDay + TimeSpan.FromHours(hours);
    }

    private static ProgramScheduleItem Flood(Collection mediaCollection, PlaybackOrder playbackOrder) =>
        new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = mediaCollection,
            CollectionId = mediaCollection.Id,
            StartTime = null,
            PlaybackOrder = playbackOrder
        };

    private static Movie TestMovie(int id, TimeSpan duration, DateTime aired) =>
        new()
        {
            Id = id,
            MovieMetadata = new List<MovieMetadata> { new() { ReleaseDate = aired } },
            MediaVersions = new List<MediaVersion>
            {
                new()
                {
                    Duration = duration, MediaFiles = new List<MediaFile>
                    {
                        new() { Path = $"/fake/path/{id}" }
                    }
                }
            }
        };

    private TestData TestDataFloodForItems(
        List<MediaItem> mediaItems,
        PlaybackOrder playbackOrder,
        Mock<IConfigElementRepository> configMock = null)
    {
        var mediaCollection = new Collection
        {
            Id = 1,
            MediaItems = mediaItems
        };

        Mock<IConfigElementRepository> configRepo = configMock ?? new Mock<IConfigElementRepository>();

        var collectionRepo = new FakeMediaCollectionRepository(Map((mediaCollection.Id, mediaItems)));
        var televisionRepo = new FakeTelevisionRepository();
        var artistRepo = new Mock<IArtistRepository>();
        var builder = new PlayoutBuilder(
            configRepo.Object,
            collectionRepo,
            televisionRepo,
            artistRepo.Object,
            _logger);

        var items = new List<ProgramScheduleItem> { Flood(mediaCollection, playbackOrder) };

        var playout = new Playout
        {
            Id = 1,
            ProgramSchedule = new ProgramSchedule { Items = items },
            Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
            Items = new List<PlayoutItem>(),
            ProgramScheduleAnchors = new List<PlayoutProgramScheduleAnchor>()
        };

        return new TestData(builder, playout);
    }

    private record TestData(PlayoutBuilder Builder, Playout Playout);
}
