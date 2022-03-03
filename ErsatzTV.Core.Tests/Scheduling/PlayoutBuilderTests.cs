using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
    [Timeout(2000)]
    public async Task OnlyZeroDurationItem_Should_Abort()
    {
        var mediaItems = new List<MediaItem>
        {
            TestMovie(1, TimeSpan.Zero, DateTime.Today)
        };

        (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Random);
        DateTimeOffset start = HoursAfterMidnight(0);
        DateTimeOffset finish = start + TimeSpan.FromHours(6);

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

        result.Items.Should().BeNull();
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

        result.Items.Count.Should().Be(1);
        result.Items.Head().MediaItemId.Should().Be(2);
        result.Items.Head().StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
        result.Items.Head().FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
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
        result.Items.Head().StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
        result.Items.Head().FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
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
        result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
        result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(6));
        result.Items[1].FinishOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(12));
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
        result.Items[0].StartOffset.TimeOfDay.Should().Be(TimeSpan.Zero);
        result.Items[0].MediaItemId.Should().Be(1);
        result.Items[1].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(1));
        result.Items[1].MediaItemId.Should().Be(2);
        result.Items[2].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(2));
        result.Items[2].MediaItemId.Should().Be(1);
        result.Items[3].StartOffset.TimeOfDay.Should().Be(TimeSpan.FromHours(3));
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

        result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));

        result.ProgramScheduleAnchors.Count.Should().Be(1);
        result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);

        DateTimeOffset start2 = HoursAfterMidnight(1);
        DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

        Playout result2 = await builder.BuildPlayoutItems(playout, start2, finish2);

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

        (PlayoutBuilder builder, Playout playout) = TestDataFloodForItems(mediaItems, PlaybackOrder.Chronological);
        DateTimeOffset start = HoursAfterMidnight(0);
        DateTimeOffset finish = start + TimeSpan.FromHours(6);

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

        result.Items.Count.Should().Be(1);
        result.Items.Head().MediaItemId.Should().Be(1);

        result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));
        result.ProgramScheduleAnchors.Count.Should().Be(1);
        result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(1);

        DateTimeOffset start2 = HoursAfterMidnight(1);
        DateTimeOffset finish2 = start2 + TimeSpan.FromHours(12);

        Playout result2 = await builder.BuildPlayoutItems(playout, start2, finish2);

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
        result.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));

        result.ProgramScheduleAnchors.Count.Should().Be(1);
        result.ProgramScheduleAnchors.Head().EnumeratorState.Index.Should().Be(0);

        int firstSeedValue = result.ProgramScheduleAnchors.Head().EnumeratorState.Seed;

        DateTimeOffset start2 = HoursAfterMidnight(0);
        DateTimeOffset finish2 = start2 + TimeSpan.FromHours(6);

        Playout result2 = await builder.BuildPlayoutItems(playout, start2, finish2, true);

        result2.Items.Count.Should().Be(6);
        result2.Anchor.NextStartOffset.Should().Be(DateTime.Today.AddHours(6));

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
                Index = 1,
                Collection = floodCollection,
                CollectionId = floodCollection.Id,
                StartTime = null,
                PlaybackOrder = PlaybackOrder.Chronological
            },
            new ProgramScheduleItemOne
            {
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
            Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

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
                Index = 1,
                Collection = floodCollection,
                CollectionId = floodCollection.Id,
                StartTime = null,
                PlaybackOrder = PlaybackOrder.Chronological
            },
            new ProgramScheduleItemMultiple
            {
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
            Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

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
                Index = 1,
                Collection = floodCollection,
                CollectionId = floodCollection.Id,
                StartTime = TimeSpan.FromHours(7),
                PlaybackOrder = PlaybackOrder.Chronological
            },
            new ProgramScheduleItemOne
            {
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
            Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

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
                Index = 1,
                Collection = floodCollection,
                CollectionId = floodCollection.Id,
                StartTime = TimeSpan.FromHours(7),
                PlaybackOrder = PlaybackOrder.Chronological
            },
            new ProgramScheduleItemOne
            {
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
            }
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

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
                Index = 1,
                Collection = floodCollection,
                CollectionId = floodCollection.Id,
                StartTime = null,
                PlaybackOrder = PlaybackOrder.Chronological
            },
            new ProgramScheduleItemDuration
            {
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
            Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

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
                Index = 1,
                Collection = multipleCollection,
                CollectionId = multipleCollection.Id,
                StartTime = null,
                Count = 2,
                PlaybackOrder = PlaybackOrder.Chronological
            },
            new ProgramScheduleItemDuration
            {
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
            Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

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
            }
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

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
                TestMovie(5, TimeSpan.FromHours(1), new DateTime(2020, 1, 1)),
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

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
            }
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

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
            
        Playout result = await builder.BuildPlayoutItems(playout, start, finish);
            
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
                TailMode = TailMode.None,
            }
        };

        var playout = new Playout
        {
            ProgramSchedule = new ProgramSchedule
            {
                Items = items
            },
            Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
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

        Playout result = await builder.BuildPlayoutItems(playout, start, finish);

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
    }

    private static DateTimeOffset HoursAfterMidnight(int hours)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        return now - now.TimeOfDay + TimeSpan.FromHours(hours);
    }

    private static ProgramScheduleItem Flood(Collection mediaCollection, PlaybackOrder playbackOrder) =>
        new ProgramScheduleItemFlood
        {
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
                new() { Duration = duration }
            }
        };

    private TestData TestDataFloodForItems(List<MediaItem> mediaItems, PlaybackOrder playbackOrder)
    {
        var mediaCollection = new Collection
        {
            Id = 1,
            MediaItems = mediaItems
        };

        var configRepo = new Mock<IConfigElementRepository>();
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
            Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" }
        };

        return new TestData(builder, playout);
    }

    private record TestData(PlayoutBuilder Builder, Playout Playout);
}