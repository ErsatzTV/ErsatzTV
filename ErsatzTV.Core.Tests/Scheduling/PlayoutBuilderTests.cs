using Destructurama;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Tests.Fakes;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class PlayoutBuilderTests
{
    [SetUp]
    public void SetUp() => _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    private readonly ILogger<PlayoutBuilder> _logger;

    public PlayoutBuilderTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .Destructure.UsingAttributes()
            .CreateLogger();

        ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

        _logger = loggerFactory.CreateLogger<PlayoutBuilder>();
    }

    private CancellationToken _cancellationToken;

    [TestFixture]
    public class NewPlayout : PlayoutBuilderTests
    {
        [Test]
        public async Task OnlyZeroDurationItem_Should_Abort()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.Zero, DateTime.Today)
            };

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildMode.Reset, _cancellationToken);

            result.AddedItems.ShouldBeEmpty();
        }

        [Test]
        public async Task ZeroDurationItem_Should_BeSkipped()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.Zero, DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today)
            };

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) = TestDataFloodForItems(mediaItems, PlaybackOrder.Random);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems.Head().MediaItemId.ShouldBe(2);
            result.AddedItems.Head().StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems.Head().FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
        }

        [Test]
        public async Task OnlyFileNotFoundItem_Should_Abort()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), DateTime.Today)
            };

            mediaItems[0].State = MediaItemState.FileNotFound;

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            configRepo
                .GetValue<bool>(Arg.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key))
                .Returns(Some(true));

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildMode.Reset, _cancellationToken);

            result.AddedItems.ShouldBeEmpty();
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

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            configRepo
                .GetValue<bool>(Arg.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key))
                .Returns(Some(true));

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems.Head().MediaItemId.ShouldBe(2);
            result.AddedItems.Head().StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems.Head().FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
        }

        [Test]
        public async Task OnlyUnavailableItem_Should_Abort()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), DateTime.Today)
            };

            mediaItems[0].State = MediaItemState.Unavailable;

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            configRepo
                .GetValue<bool>(Arg.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key))
                .Returns(Some(true));

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildMode.Reset, _cancellationToken);

            result.AddedItems.ShouldBeEmpty();
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

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            configRepo
                .GetValue<bool>(Arg.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key))
                .Returns(Some(true));

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems.Head().MediaItemId.ShouldBe(2);
            result.AddedItems.Head().StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems.Head().FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
        }

        [Test]
        public async Task FileNotFound_Should_NotBeSkippedIfConfigured()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today)
            };

            mediaItems[0].State = MediaItemState.FileNotFound;

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            configRepo
                .GetValue<bool>(Arg.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key))
                .Returns(Some(false));

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems.Head().StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems.Head().FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
        }

        [Test]
        public async Task Unavailable_Should_NotBeSkippedIfConfigured()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today)
            };

            mediaItems[0].State = MediaItemState.Unavailable;

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            configRepo
                .GetValue<bool>(Arg.Is<ConfigElementKey>(k => k.Key == ConfigElementKey.PlayoutSkipMissingItems.Key))
                .Returns(Some(false));

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Random, configRepo);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems.Head().StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems.Head().FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
        }

        [Test]
        public async Task InitialFlood_Should_StartAtMidnight()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today)
            };

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) = TestDataFloodForItems(mediaItems, PlaybackOrder.Random);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems.Head().StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems.Head().FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
        }

        [Test]
        public async Task InitialFlood_Should_StartAtMidnight_With_LateStart()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today)
            };

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) = TestDataFloodForItems(mediaItems, PlaybackOrder.Random);
            DateTimeOffset start = HoursAfterMidnight(1);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(2);
            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result.AddedItems[1].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(12));
        }

        [Test]
        public async Task ChronologicalContent_Should_CreateChronologicalItems()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
            };

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(4);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(4);
            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[1].MediaItemId.ShouldBe(2);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[2].MediaItemId.ShouldBe(1);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[3].MediaItemId.ShouldBe(2);

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(4));
        }

        [Test]
        public async Task ChronologicalFlood_Should_AnchorAndMaintainExistingPlayout()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today.AddHours(1))
            };

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems.Head().MediaItemId.ShouldBe(1);

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));

            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(1);

            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            PlayoutBuildResult result2 = await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start2,
                finish2,
                _cancellationToken);

            result2.AddedItems.Count.ShouldBe(1);
            result2.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result2.AddedItems[0].MediaItemId.ShouldBe(2);

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(12));
            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);
        }

        [Test]
        public async Task ChronologicalFlood_Should_AnchorAndReturnNewPlayoutItems()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today.AddHours(1))
            };

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems.Head().MediaItemId.ShouldBe(1);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(6));
            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(1);

            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(12);

            PlayoutBuildResult result2 = await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start2,
                finish2,
                _cancellationToken);

            result2.AddedItems.Count.ShouldBe(2);
            result2.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result2.AddedItems[0].MediaItemId.ShouldBe(2);
            result2.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result2.AddedItems[1].MediaItemId.ShouldBe(1);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(18));
            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(1);
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

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) = TestDataFloodForItems(mediaItems, PlaybackOrder.Shuffle);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(6);
            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(6));

            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);

            int firstSeedValue = playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            PlayoutBuildResult result2 = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start2, finish2, _cancellationToken);

            result2.AddedItems.Count.ShouldBe(6);
            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(6));

            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);

            int secondSeedValue = playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            firstSeedValue.ShouldNotBe(secondSeedValue);
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

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) = TestDataFloodForItems(mediaItems, PlaybackOrder.Shuffle);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(6);
            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed.ShouldBeGreaterThan(0);

            int firstSeedValue = playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            PlayoutBuildResult result2 = await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start2,
                finish2,
                _cancellationToken);

            int secondSeedValue = playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            firstSeedValue.ShouldBe(secondSeedValue);

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_One()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                ]
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems = [TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1))]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(5);
            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[1].MediaItemId.ShouldBe(2);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[2].MediaItemId.ShouldBe(1);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[3].MediaItemId.ShouldBe(3);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(5));
            result.AddedItems[4].MediaItemId.ShouldBe(2);

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_One_Multiple_Days()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                ]
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems = [TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1))]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(30);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(28);
            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[1].MediaItemId.ShouldBe(2);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[2].MediaItemId.ShouldBe(1);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[3].MediaItemId.ShouldBe(3);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(5));
            result.AddedItems[4].MediaItemId.ShouldBe(2);
            result.AddedItems[5].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result.AddedItems[5].MediaItemId.ShouldBe(1);
            result.AddedItems[6].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(7));
            result.AddedItems[6].MediaItemId.ShouldBe(2);
            result.AddedItems[7].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(8));
            result.AddedItems[7].MediaItemId.ShouldBe(1);
            result.AddedItems[8].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(9));
            result.AddedItems[8].MediaItemId.ShouldBe(2);
            result.AddedItems[9].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(10));
            result.AddedItems[9].MediaItemId.ShouldBe(1);
            result.AddedItems[10].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(11));
            result.AddedItems[10].MediaItemId.ShouldBe(2);
            result.AddedItems[11].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result.AddedItems[11].MediaItemId.ShouldBe(1);
            result.AddedItems[12].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(13));
            result.AddedItems[12].MediaItemId.ShouldBe(2);
            result.AddedItems[13].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(14));
            result.AddedItems[13].MediaItemId.ShouldBe(1);
            result.AddedItems[14].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(15));
            result.AddedItems[14].MediaItemId.ShouldBe(2);
            result.AddedItems[15].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(16));
            result.AddedItems[15].MediaItemId.ShouldBe(1);
            result.AddedItems[16].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(17));
            result.AddedItems[16].MediaItemId.ShouldBe(2);
            result.AddedItems[17].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(18));
            result.AddedItems[17].MediaItemId.ShouldBe(1);
            result.AddedItems[18].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(19));
            result.AddedItems[18].MediaItemId.ShouldBe(2);
            result.AddedItems[19].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(20));
            result.AddedItems[19].MediaItemId.ShouldBe(1);
            result.AddedItems[20].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(21));
            result.AddedItems[20].MediaItemId.ShouldBe(2);
            result.AddedItems[21].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(22));
            result.AddedItems[21].MediaItemId.ShouldBe(1);
            result.AddedItems[22].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(23));
            result.AddedItems[22].MediaItemId.ShouldBe(2);
            result.AddedItems[23].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[23].MediaItemId.ShouldBe(1);
            result.AddedItems[24].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[24].MediaItemId.ShouldBe(2);
            result.AddedItems[25].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[25].MediaItemId.ShouldBe(1);
            result.AddedItems[26].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[26].MediaItemId.ShouldBe(3);
            result.AddedItems[27].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(5));
            result.AddedItems[27].MediaItemId.ShouldBe(2);

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(30));
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_Multiple()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                ]
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems =
                [
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 2))
                ]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(7);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(6);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[1].MediaItemId.ShouldBe(2);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[2].MediaItemId.ShouldBe(1);

            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[3].MediaItemId.ShouldBe(3);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(5));
            result.AddedItems[4].MediaItemId.ShouldBe(4);

            result.AddedItems[5].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result.AddedItems[5].MediaItemId.ShouldBe(2);

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(7));
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_Multiple_With_Gap()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromMinutes(50), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                ]
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems =
                [
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 2))
                ]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(7);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(6);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromMinutes(50));
            result.AddedItems[1].MediaItemId.ShouldBe(2);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromMinutes(50 + 60));
            result.AddedItems[2].MediaItemId.ShouldBe(1);

            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[3].MediaItemId.ShouldBe(3);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(5));
            result.AddedItems[4].MediaItemId.ShouldBe(4);

            result.AddedItems[5].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result.AddedItems[5].MediaItemId.ShouldBe(2);

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(7));
        }

        [Test]
        public async Task FloodContent_Should_FloodWithFixedStartTime()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                ]
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems =
                [
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 2))
                ]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(24);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(6);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(7));
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(8));
            result.AddedItems[1].MediaItemId.ShouldBe(2);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(9));
            result.AddedItems[2].MediaItemId.ShouldBe(1);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(10));
            result.AddedItems[3].MediaItemId.ShouldBe(2);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(11));
            result.AddedItems[4].MediaItemId.ShouldBe(1);

            result.AddedItems[5].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result.AddedItems[5].MediaItemId.ShouldBe(3);

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(31));
        }

        [Test]
        public async Task ContinuePlayout_FloodContent_Should_FloodWithFixedStartTime_FromAnchor()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                ]
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems =
                [
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 2))
                ]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(32);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Continue, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(5);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(9));
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(10));
            result.AddedItems[1].MediaItemId.ShouldBe(2);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(11));
            result.AddedItems[2].MediaItemId.ShouldBe(1);

            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result.AddedItems[3].MediaItemId.ShouldBe(3);

            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(7));
            result.AddedItems[4].MediaItemId.ShouldBe(2);

            playout.Anchor.InFlood.ShouldBeTrue();

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(32));
        }

        [Test]
        public async Task FloodContent_Should_FloodAroundFixedContent_DurationWithoutOfflineTail()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                ]
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems =
                [
                    TestMovie(3, TimeSpan.FromHours(0.75), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1.5), new DateTime(2020, 1, 2))
                ]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(7);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[1].MediaItemId.ShouldBe(2);

            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[2].MediaItemId.ShouldBe(3);

            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2.75));
            result.AddedItems[3].MediaItemId.ShouldBe(1);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3.75));
            result.AddedItems[4].MediaItemId.ShouldBe(2);

            result.AddedItems[5].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(4.75));
            result.AddedItems[5].MediaItemId.ShouldBe(1);
            result.AddedItems[6].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(5.75));
            result.AddedItems[6].MediaItemId.ShouldBe(2);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(6.75));
        }

        [Test]
        public async Task MultipleContent_Should_WrapAroundDynamicContent_DurationWithoutOfflineTail()
        {
            var multipleCollection = new Collection
            {
                Id = 1,
                Name = "Multiple Items",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                ]
            };

            var dynamicCollection = new Collection
            {
                Id = 2,
                Name = "Dynamic Items",
                MediaItems =
                [
                    TestMovie(3, TimeSpan.FromHours(0.75), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1.5), new DateTime(2020, 1, 2))
                ]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(6);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[1].MediaItemId.ShouldBe(2);

            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[2].MediaItemId.ShouldBe(3);

            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2.75));
            result.AddedItems[3].MediaItemId.ShouldBe(1);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3.75));
            result.AddedItems[4].MediaItemId.ShouldBe(2);

            result.AddedItems[5].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(4.75));
            result.AddedItems[5].MediaItemId.ShouldBe(4);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(6.25));
        }

        [Test]
        public async Task ContinuePlayout_Alternating_MultipleContent_Should_Maintain_Counts()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Multiple Items 1",
                MediaItems = [TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))]
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Multiple Items 2",
                MediaItems = [TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(5);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Continue, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(4);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[1].MediaItemId.ShouldBe(1);

            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[2].MediaItemId.ShouldBe(2);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(4));
            result.AddedItems[3].MediaItemId.ShouldBe(2);

            playout.Anchor.ScheduleItemsEnumeratorState.Index.ShouldBe(1);
            playout.Anchor.MultipleRemaining.ShouldBe(1);
            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(5));
        }

        [Test]
        public async Task Multiple_Mode_Collection_Size()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Multiple Items 1",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(3, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                ]
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Multiple Items 2",
                MediaItems =
                [
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(5, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                ]
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
                    MultipleMode = MultipleMode.CollectionSize,
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
                    MultipleMode = MultipleMode.CollectionSize,
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(5);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(5);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(0));
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[1].MediaItemId.ShouldBe(2);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[2].MediaItemId.ShouldBe(3);

            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[3].MediaItemId.ShouldBe(4);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(4));
            result.AddedItems[4].MediaItemId.ShouldBe(5);

            playout.Anchor.ScheduleItemsEnumeratorState.Index.ShouldBe(0);
            playout.Anchor.MultipleRemaining.ShouldBeNull();
            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(5));
        }

        [Test]
        public async Task ContinuePlayout_Alternating_Duration_Should_Complete_Duration()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems = [TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))]
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Duration Items 2",
                MediaItems = [TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(5);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Continue, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(5);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[1].MediaItemId.ShouldBe(1);

            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[2].MediaItemId.ShouldBe(2);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(4));
            result.AddedItems[3].MediaItemId.ShouldBe(2);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(5));
            result.AddedItems[4].MediaItemId.ShouldBe(2);

            playout.Anchor.ScheduleItemsEnumeratorState.Index.ShouldBe(0);
            playout.Anchor.DurationFinish.ShouldBeNull();
            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
        }

        [Test]
        public async Task Alternating_Duration_With_Filler_Should_Alternate_Schedule_Items()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems = [TestMovie(1, TimeSpan.FromMinutes(55), new DateTime(2020, 1, 1))]
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Duration Items 2",
                MediaItems = [TestMovie(2, TimeSpan.FromMinutes(55), new DateTime(2020, 1, 1))]
            };

            var collectionThree = new Collection
            {
                Id = 3,
                Name = "Filler Items",
                MediaItems = [TestMovie(3, TimeSpan.FromMinutes(5), new DateTime(2020, 1, 1))]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(12);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromMinutes(0));
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromMinutes(55));
            result.AddedItems[1].MediaItemId.ShouldBe(1);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(new TimeSpan(1, 50, 0));
            result.AddedItems[2].MediaItemId.ShouldBe(1);

            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(new TimeSpan(2, 45, 0));
            result.AddedItems[3].MediaItemId.ShouldBe(3);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(new TimeSpan(2, 50, 0));
            result.AddedItems[4].MediaItemId.ShouldBe(3);
            result.AddedItems[5].StartOffset.TimeOfDay.ShouldBe(new TimeSpan(2, 55, 0));
            result.AddedItems[5].MediaItemId.ShouldBe(3);

            result.AddedItems[6].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[6].MediaItemId.ShouldBe(2);
            result.AddedItems[7].StartOffset.TimeOfDay.ShouldBe(new TimeSpan(3, 55, 0));
            result.AddedItems[7].MediaItemId.ShouldBe(2);
            result.AddedItems[8].StartOffset.TimeOfDay.ShouldBe(new TimeSpan(4, 50, 0));
            result.AddedItems[8].MediaItemId.ShouldBe(2);

            result.AddedItems[9].StartOffset.TimeOfDay.ShouldBe(new TimeSpan(5, 45, 0));
            result.AddedItems[9].MediaItemId.ShouldBe(3);
            result.AddedItems[10].StartOffset.TimeOfDay.ShouldBe(new TimeSpan(5, 50, 0));
            result.AddedItems[10].MediaItemId.ShouldBe(3);
            result.AddedItems[11].StartOffset.TimeOfDay.ShouldBe(new TimeSpan(5, 55, 0));
            result.AddedItems[11].MediaItemId.ShouldBe(3);

            playout.Anchor.ScheduleItemsEnumeratorState.Index.ShouldBe(0);
            playout.Anchor.DurationFinish.ShouldBeNull();

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
        }

        [Test]
        public async Task Multiple_With_Filler_Should_Keep_Filler_After_End_Of_Playout()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems = [TestMovie(1, TimeSpan.FromMinutes(61), new DateTime(2020, 1, 1))]
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Filler Items",
                MediaItems = [TestMovie(2, TimeSpan.FromMinutes(4), new DateTime(2020, 1, 1))]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(1);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(2);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromMinutes(0));
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromMinutes(61));
            result.AddedItems[1].MediaItemId.ShouldBe(2);

            playout.Anchor.ScheduleItemsEnumeratorState.Index.ShouldBe(0);
            playout.Anchor.DurationFinish.ShouldBeNull();
            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddMinutes(65));
        }

        [Test]
        public async Task Duration_Should_Skip_Items_That_Are_Too_Long()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))
                ]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(6);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(0));
            result.AddedItems[0].MediaItemId.ShouldBe(2);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[1].MediaItemId.ShouldBe(4);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[2].MediaItemId.ShouldBe(2);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[3].MediaItemId.ShouldBe(4);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(4));
            result.AddedItems[4].MediaItemId.ShouldBe(2);
            result.AddedItems[5].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(5));
            result.AddedItems[5].MediaItemId.ShouldBe(4);

            playout.Anchor.ScheduleItemsEnumeratorState.Index.ShouldBe(0);
            playout.Anchor.DurationFinish.ShouldBeNull();
            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
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

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) = TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromDays(2);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(8);
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[0].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result.AddedItems[1].MediaItemId.ShouldBe(2);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result.AddedItems[1].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result.AddedItems[2].MediaItemId.ShouldBe(3);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result.AddedItems[2].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(18));
            result.AddedItems[3].MediaItemId.ShouldBe(1);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(18));
            result.AddedItems[3].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[4].MediaItemId.ShouldBe(2);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[4].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result.AddedItems[5].MediaItemId.ShouldBe(3);
            result.AddedItems[5].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result.AddedItems[5].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result.AddedItems[6].MediaItemId.ShouldBe(1);
            result.AddedItems[6].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result.AddedItems[6].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(18));
            result.AddedItems[7].MediaItemId.ShouldBe(2);
            result.AddedItems[7].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(18));
            result.AddedItems[7].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);

            playout.ProgramScheduleAnchors.Count.ShouldBe(2);
            playout.ProgramScheduleAnchors.Count(a => a.EnumeratorState.Index == 4 % 3).ShouldBe(1);
            playout.ProgramScheduleAnchors.Count(a => a.EnumeratorState.Index == 8 % 3).ShouldBe(1);

            int seed = playout.ProgramScheduleAnchors[0].EnumeratorState.Seed;
            playout.ProgramScheduleAnchors.All(a => a.EnumeratorState.Seed == seed).ShouldBeTrue();

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddDays(2));
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

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) = TestDataFloodForItems(mediaItems, PlaybackOrder.Shuffle);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(6);
            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(6));

            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);

            int firstSeedValue = playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            PlayoutBuildResult result2 = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start2, finish2, _cancellationToken);

            result2.AddedItems.Count.ShouldBe(6);
            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(6));

            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);

            int secondSeedValue = playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            firstSeedValue.ShouldNotBe(secondSeedValue);
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
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(6), new DateTime(2002, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(6), new DateTime(2003, 1, 1)),
                    TestMovie(3, TimeSpan.FromHours(6), new DateTime(2004, 1, 1))
                ]
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

                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
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
                    Playout = playout
                });

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(24);
            DateTimeOffset finish = start + TimeSpan.FromDays(1);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Refresh, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(4);
            result.AddedItems[0].MediaItemId.ShouldBe(2);
            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);
            result.AddedItems[0].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result.AddedItems[1].MediaItemId.ShouldBe(3);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result.AddedItems[1].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result.AddedItems[2].MediaItemId.ShouldBe(1);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result.AddedItems[2].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(18));
            result.AddedItems[3].MediaItemId.ShouldBe(2);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(18));
            result.AddedItems[3].FinishOffset.TimeOfDay.ShouldBe(TimeSpan.Zero);

            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(48));
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

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems.Head().MediaItemId.ShouldBe(1);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(6));

            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(1);

            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            PlayoutBuildResult result2 = await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start2,
                finish2,
                _cancellationToken);

            result2.AddedItems.Count.ShouldBe(1);
            result2.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result2.AddedItems[0].MediaItemId.ShouldBe(2);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(12));
            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);
        }

        [Test]
        public async Task ChronologicalFlood_Should_AnchorAndReturnNewPlayoutItems()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today.AddHours(1))
            };

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems.Head().MediaItemId.ShouldBe(1);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(6));
            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(1);

            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(12);

            PlayoutBuildResult result2 = await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start2,
                finish2,
                _cancellationToken);

            result2.AddedItems.Count.ShouldBe(2);
            result2.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(6));
            result2.AddedItems[0].MediaItemId.ShouldBe(2);
            result2.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result2.AddedItems[1].MediaItemId.ShouldBe(1);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(18));
            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(1);
        }

        [Test]
        public async Task ChronologicalFlood_Should_AnchorAndReturnNewPlayoutItems_MultiDay()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(6), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(6), DateTime.Today.AddHours(1))
            };

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromDays(1);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(4);
            result.AddedItems.Map(i => i.MediaItemId).ToList().ShouldBe([1, 2, 1, 2]);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddDays(1));
            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);

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
                    SmartCollection = headAnchor.SmartCollection,
                    SmartCollectionId = headAnchor.SmartCollectionId
                });

            // continue 1h later
            DateTimeOffset start2 = HoursAfterMidnight(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromDays(1);

            result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Continue, start2, finish2, _cancellationToken);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(0));
            result.AddedItems[0].MediaItemId.ShouldBe(1);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(30));

            playout.ProgramScheduleAnchors.Count.ShouldBe(2);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(1);

            // continue 1h later
            DateTimeOffset start3 = HoursAfterMidnight(2);
            DateTimeOffset finish3 = start3 + TimeSpan.FromDays(1);

            result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Continue, start3, finish3, _cancellationToken);

            result.AddedItems.Count.ShouldBe(0);

            playout.Anchor.NextStartOffset.ShouldBe(DateTime.Today.AddHours(30));

            playout.ProgramScheduleAnchors.Count.ShouldBe(2);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(1);
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

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) = TestDataFloodForItems(mediaItems, PlaybackOrder.Shuffle);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(6);
            playout.ProgramScheduleAnchors.Count.ShouldBe(1);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed.ShouldBeGreaterThan(0);
            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);

            int firstSeedValue = playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            PlayoutBuildResult result2 = await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start2,
                finish2,
                _cancellationToken);

            int secondSeedValue = playout.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

            firstSeedValue.ShouldBe(secondSeedValue);

            playout.ProgramScheduleAnchors.Head().EnumeratorState.Index.ShouldBe(0);
        }

        [Test]
        public async Task ShuffleFlood_Should_MaintainRandomSeed_MultipleDays()
        {
            var mediaItems = new List<MediaItem>();
            for (var i = 1; i <= 25; i++)
            {
                mediaItems.Add(TestMovie(i, TimeSpan.FromMinutes(55), DateTime.Today.AddHours(i)));
            }

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) = TestDataFloodForItems(mediaItems, PlaybackOrder.Shuffle);
            DateTimeOffset start = HoursAfterMidnight(0).AddSeconds(5);
            DateTimeOffset finish = start + TimeSpan.FromDays(2);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(53);
            playout.ProgramScheduleAnchors.Count.ShouldBe(2);

            playout.ProgramScheduleAnchors.All(x => x.AnchorDate is not null).ShouldBeTrue();
            PlayoutProgramScheduleAnchor lastCheckpoint = playout.ProgramScheduleAnchors
                .OrderByDescending(a => a.AnchorDate ?? DateTime.MinValue)
                .First();
            lastCheckpoint.EnumeratorState.Seed.ShouldBeGreaterThan(0);
            lastCheckpoint.EnumeratorState.Index.ShouldBe(3);

            // we need to mess up the ordering to trigger the problematic behavior
            // this simulates the way the rows are loaded with EF
            PlayoutProgramScheduleAnchor oldest = playout.ProgramScheduleAnchors.OrderByDescending(a => a.AnchorDate)
                .Last();
            PlayoutProgramScheduleAnchor newest = playout.ProgramScheduleAnchors.OrderByDescending(a => a.AnchorDate)
                .First();

            playout.ProgramScheduleAnchors =
            [
                oldest,
                newest
            ];

            int firstSeedValue = lastCheckpoint.EnumeratorState.Seed;

            DateTimeOffset start2 = start.AddHours(1);
            DateTimeOffset finish2 = start2 + TimeSpan.FromDays(2);

            PlayoutBuildResult result2 = await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start2,
                finish2,
                _cancellationToken);

            PlayoutProgramScheduleAnchor continueAnchor =
                playout.ProgramScheduleAnchors.First(x => x.AnchorDate is null);
            int secondSeedValue = continueAnchor.EnumeratorState.Seed;

            // the continue anchor should have the same seed as the most recent (last) checkpoint from the first run
            firstSeedValue.ShouldBe(secondSeedValue);
        }

        [Test]
        public async Task ShuffleFlood_MultipleSmartCollections_Should_MaintainRandomSeed()
        {
            var mediaItems = new List<MediaItem>
            {
                TestMovie(1, TimeSpan.FromHours(1), DateTime.Today),
                TestMovie(2, TimeSpan.FromHours(1), DateTime.Today.AddHours(1)),
                TestMovie(3, TimeSpan.FromHours(1), DateTime.Today.AddHours(3))
            };

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForSmartCollectionItems(mediaItems, PlaybackOrder.Shuffle);
            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(6);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(6);
            playout.ProgramScheduleAnchors.Count.ShouldBe(2);
            PlayoutProgramScheduleAnchor primaryAnchor =
                playout.ProgramScheduleAnchors.First(a => a.SmartCollectionId == 1);
            primaryAnchor.EnumeratorState.Seed.ShouldBeGreaterThan(0);
            primaryAnchor.EnumeratorState.Index.ShouldBe(0);

            int firstSeedValue = primaryAnchor.EnumeratorState.Seed;

            DateTimeOffset start2 = HoursAfterMidnight(0);
            DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

            PlayoutBuildResult result2 = await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start2,
                finish2,
                _cancellationToken);

            primaryAnchor = playout.ProgramScheduleAnchors.First(a => a.SmartCollectionId == 1);
            int secondSeedValue = primaryAnchor.EnumeratorState.Seed;

            firstSeedValue.ShouldBe(secondSeedValue);

            primaryAnchor.EnumeratorState.Index.ShouldBe(0);
        }

        [Test]
        public async Task ShuffleFlood_MultipleSmartCollections_Should_MaintainRandomSeed_MultipleDays()
        {
            var mediaItems = new List<MediaItem>();
            for (var i = 1; i <= 100; i++)
            {
                mediaItems.Add(TestMovie(i, TimeSpan.FromMinutes(55), DateTime.Today.AddHours(i)));
            }

            (PlayoutBuilder builder, Playout playout, PlayoutReferenceData referenceData) =
                TestDataFloodForSmartCollectionItems(mediaItems, PlaybackOrder.Shuffle);
            DateTimeOffset start = HoursAfterMidnight(0).AddSeconds(5);
            DateTimeOffset finish = start + TimeSpan.FromDays(2);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(53);
            playout.ProgramScheduleAnchors.Count.ShouldBe(4);

            playout.ProgramScheduleAnchors.All(x => x.AnchorDate is not null).ShouldBeTrue();
            PlayoutProgramScheduleAnchor lastCheckpoint = playout.ProgramScheduleAnchors
                .Filter(psa => psa.SmartCollectionId == 1)
                .OrderByDescending(a => a.AnchorDate ?? DateTime.MinValue)
                .First();
            lastCheckpoint.EnumeratorState.Seed.ShouldBeGreaterThan(0);
            lastCheckpoint.EnumeratorState.Index.ShouldBe(53);

            int firstSeedValue = lastCheckpoint.EnumeratorState.Seed;

            for (var i = 1; i < 20; i++)
            {
                DateTimeOffset start2 = start.AddHours(i);
                DateTimeOffset finish2 = start2 + TimeSpan.FromDays(2);

                PlayoutBuildResult result2 = await builder.Build(
                    playout,
                    referenceData,
                    PlayoutBuildResult.Empty,
                    PlayoutBuildMode.Continue,
                    start2,
                    finish2,
                    _cancellationToken);

                PlayoutProgramScheduleAnchor continueAnchor =
                    playout.ProgramScheduleAnchors
                        .Filter(psa => psa.SmartCollectionId == 1)
                        .First(x => x.AnchorDate is null);
                int secondSeedValue = continueAnchor.EnumeratorState.Seed;

                // the continue anchor should have the same seed as the most recent (last) checkpoint from the first run
                firstSeedValue.ShouldBe(secondSeedValue);
            }
        }

        [Test]
        public async Task FloodContent_Should_FloodWithFixedStartTime_FromAnchor()
        {
            var floodCollection = new Collection
            {
                Id = 1,
                Name = "Flood Items",
                MediaItems =
                [
                    TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
                    TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 2, 1))
                ]
            };

            var fixedCollection = new Collection
            {
                Id = 2,
                Name = "Fixed Items",
                MediaItems =
                [
                    TestMovie(3, TimeSpan.FromHours(2), new DateTime(2020, 1, 1)),
                    TestMovie(4, TimeSpan.FromHours(1), new DateTime(2020, 1, 2))
                ]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(32);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Continue, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(5);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(9));
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(10));
            result.AddedItems[1].MediaItemId.ShouldBe(2);
            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(11));
            result.AddedItems[2].MediaItemId.ShouldBe(1);

            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
            result.AddedItems[3].MediaItemId.ShouldBe(3);

            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(7));
            result.AddedItems[4].MediaItemId.ShouldBe(2);

            playout.Anchor.InFlood.ShouldBeTrue();
            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(32));
        }

        [Test]
        public async Task Alternating_MultipleContent_Should_Maintain_Counts()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Multiple Items 1",
                MediaItems = [TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))]
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Multiple Items 2",
                MediaItems = [TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(5);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Continue, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(4);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[1].MediaItemId.ShouldBe(1);

            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[2].MediaItemId.ShouldBe(2);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(4));
            result.AddedItems[3].MediaItemId.ShouldBe(2);

            playout.Anchor.ScheduleItemsEnumeratorState.Index.ShouldBe(1);
            playout.Anchor.MultipleRemaining.ShouldBe(1);
            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(5));
        }

        [Test]
        public async Task Alternating_Duration_Should_Complete_Duration()
        {
            var collectionOne = new Collection
            {
                Id = 1,
                Name = "Duration Items 1",
                MediaItems = [TestMovie(1, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))]
            };

            var collectionTwo = new Collection
            {
                Id = 2,
                Name = "Duration Items 2",
                MediaItems = [TestMovie(2, TimeSpan.FromHours(1), new DateTime(2020, 1, 1))]
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
                ProgramScheduleAnchors = [],
                Items = [],
                ProgramScheduleAlternates = [],
                FillGroupIndices = []
            };

            var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            var televisionRepo = new FakeTelevisionRepository();
            IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
            IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
                Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
            ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
            var builder = new PlayoutBuilder(
                configRepo,
                fakeRepository,
                televisionRepo,
                artistRepo,
                factory,
                localFileSystem,
                _logger);

            DateTimeOffset start = HoursAfterMidnight(0);
            DateTimeOffset finish = start + TimeSpan.FromHours(5);

            PlayoutBuildResult result = await builder.Build(playout, referenceData, PlayoutBuildResult.Empty, PlayoutBuildMode.Continue, start, finish, _cancellationToken);

            result.AddedItems.Count.ShouldBe(5);

            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(1));
            result.AddedItems[0].MediaItemId.ShouldBe(1);
            result.AddedItems[1].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(2));
            result.AddedItems[1].MediaItemId.ShouldBe(1);

            result.AddedItems[2].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(3));
            result.AddedItems[2].MediaItemId.ShouldBe(2);
            result.AddedItems[3].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(4));
            result.AddedItems[3].MediaItemId.ShouldBe(2);
            result.AddedItems[4].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(5));
            result.AddedItems[4].MediaItemId.ShouldBe(2);

            playout.Anchor.ScheduleItemsEnumeratorState.Index.ShouldBe(0);
            playout.Anchor.DurationFinish.ShouldBeNull();
            playout.Anchor.NextStartOffset.ShouldBe(HoursAfterMidnight(6));
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
            CollectionType = ProgramScheduleItemCollectionType.Collection,
            Collection = mediaCollection,
            CollectionId = mediaCollection.Id,
            StartTime = null,
            PlaybackOrder = playbackOrder
        };

    private static ProgramScheduleItem Flood(
        SmartCollection smartCollection,
        SmartCollection fillerCollection,
        PlaybackOrder playbackOrder) =>
        new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            CollectionType = ProgramScheduleItemCollectionType.SmartCollection,
            SmartCollection = smartCollection,
            SmartCollectionId = smartCollection.Id,
            StartTime = null,
            PlaybackOrder = playbackOrder,
            FallbackFiller = new FillerPreset
            {
                Id = 1,
                CollectionType = ProgramScheduleItemCollectionType.SmartCollection,
                SmartCollection = fillerCollection,
                SmartCollectionId = fillerCollection.Id,
                FillerKind = FillerKind.Fallback
            }
        };

    private static Movie TestMovie(int id, TimeSpan duration, DateTime aired) =>
        new()
        {
            Id = id,
            MovieMetadata = [new MovieMetadata { ReleaseDate = aired }],
            MediaVersions =
            [
                new MediaVersion
                {
                    Duration = duration, MediaFiles = [new MediaFile { Path = $"/fake/path/{id}" }]
                }
            ]
        };

    private TestData TestDataFloodForItems(
        List<MediaItem> mediaItems,
        PlaybackOrder playbackOrder,
        IConfigElementRepository configMock = null)
    {
        var mediaCollection = new Collection
        {
            Id = 1,
            MediaItems = mediaItems
        };

        IConfigElementRepository configRepo = configMock ?? Substitute.For<IConfigElementRepository>();

        var collectionRepo = new FakeMediaCollectionRepository(Map((mediaCollection.Id, mediaItems)));
        var televisionRepo = new FakeTelevisionRepository();
        IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
        IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
            Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
        ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
        var builder = new PlayoutBuilder(
            configRepo,
            collectionRepo,
            televisionRepo,
            artistRepo,
            factory,
            localFileSystem,
            _logger);

        var items = new List<ProgramScheduleItem> { Flood(mediaCollection, playbackOrder) };

        var playout = new Playout
        {
            Id = 1,
            ProgramSchedule = new ProgramSchedule { Items = items },
            Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
            Items = [],
            ProgramScheduleAnchors = [],
            ProgramScheduleAlternates = [],
            FillGroupIndices = []
        };

        var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

        return new TestData(builder, playout, referenceData);
    }

    private TestData TestDataFloodForSmartCollectionItems(
        List<MediaItem> mediaItems,
        PlaybackOrder playbackOrder,
        IConfigElementRepository configMock = null)
    {
        var mediaCollection = new SmartCollection
        {
            Id = 1,
            Query = "asdf"
        };

        var fillerCollection = new SmartCollection
        {
            Id = 2,
            Query = "qwerty"
        };

        IConfigElementRepository configRepo = configMock ?? Substitute.For<IConfigElementRepository>();

        var collectionRepo = new FakeMediaCollectionRepository(
            Map(
                (mediaCollection.Id, mediaItems),
                (fillerCollection.Id, mediaItems.Take(1).ToList())
            )
        );
        var televisionRepo = new FakeTelevisionRepository();
        IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
        IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
            Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
        ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
        var builder = new PlayoutBuilder(
            configRepo,
            collectionRepo,
            televisionRepo,
            artistRepo,
            factory,
            localFileSystem,
            _logger);

        var items = new List<ProgramScheduleItem> { Flood(mediaCollection, fillerCollection, playbackOrder) };

        var playout = new Playout
        {
            Id = 1,
            ProgramSchedule = new ProgramSchedule { Items = items },
            Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
            Items = [],
            ProgramScheduleAnchors = [],
            ProgramScheduleAlternates = [],
            FillGroupIndices = []
        };

        var referenceData = new PlayoutReferenceData(playout.Channel, Option<Deco>.None, [], [], playout.ProgramSchedule, [], []);

        return new TestData(builder, playout, referenceData);
    }

    private record TestData(PlayoutBuilder Builder, Playout Playout, PlayoutReferenceData ReferenceData);
}
