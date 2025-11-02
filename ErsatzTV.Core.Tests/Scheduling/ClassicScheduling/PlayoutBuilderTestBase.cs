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

namespace ErsatzTV.Core.Tests.Scheduling.ClassicScheduling;

public abstract class PlayoutBuilderTestBase
{
    protected readonly ILogger<PlayoutBuilder> Logger;
    protected CancellationToken CancellationToken;

    protected PlayoutBuilderTestBase()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .Destructure.UsingAttributes()
            .CreateLogger();

        ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

        Logger = loggerFactory.CreateLogger<PlayoutBuilder>();
    }

    [SetUp]
    public void SetUp() => CancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    protected static DateTimeOffset HoursAfterMidnight(int hours)
    {
        // DateTimeOffset now = DateTimeOffset.Now;
        // return now - now.TimeOfDay + TimeSpan.FromHours(hours);

        // pick a timezone that has DST and a known offset on a specific date
        TimeZoneInfo eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        //DateTime date = new DateTime(2025, 11, 2, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime date = new DateTime(2025, 10, 4, 0, 0, 0, DateTimeKind.Unspecified);
        DateTimeOffset now = new DateTimeOffset(date, eastern.GetUtcOffset(date));
        return now.Date + TimeSpan.FromHours(hours);
    }

    protected TestData TestDataFloodForItems(
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
        IRerunHelper rerunHelper = Substitute.For<IRerunHelper>();
        var builder = new PlayoutBuilder(
            configRepo,
            collectionRepo,
            televisionRepo,
            artistRepo,
            factory,
            localFileSystem,
            rerunHelper,
            Logger);

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

        var referenceData = new PlayoutReferenceData(
            playout.Channel,
            Option<Deco>.None,
            [],
            [],
            playout.ProgramSchedule,
            [],
            [],
            TimeSpan.Zero);

        return new TestData(builder, playout, referenceData);
    }

    protected static Movie TestMovie(int id, TimeSpan duration, DateTime aired) =>
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

    private static ProgramScheduleItem Flood(Collection mediaCollection, PlaybackOrder playbackOrder) =>
        new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            CollectionType = CollectionType.Collection,
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
            CollectionType = CollectionType.SmartCollection,
            SmartCollection = smartCollection,
            SmartCollectionId = smartCollection.Id,
            StartTime = null,
            PlaybackOrder = playbackOrder,
            FallbackFiller = new FillerPreset
            {
                Id = 1,
                CollectionType = CollectionType.SmartCollection,
                SmartCollection = fillerCollection,
                SmartCollectionId = fillerCollection.Id,
                FillerKind = FillerKind.Fallback
            }
        };

    protected TestData TestDataFloodForSmartCollectionItems(
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
        IRerunHelper rerunHelper = Substitute.For<IRerunHelper>();
        var builder = new PlayoutBuilder(
            configRepo,
            collectionRepo,
            televisionRepo,
            artistRepo,
            factory,
            localFileSystem,
            rerunHelper,
            Logger);

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

        var referenceData = new PlayoutReferenceData(
            playout.Channel,
            Option<Deco>.None,
            [],
            [],
            playout.ProgramSchedule,
            [],
            [],
            TimeSpan.Zero);

        return new TestData(builder, playout, referenceData);
    }

    protected record TestData(PlayoutBuilder Builder, Playout Playout, PlayoutReferenceData ReferenceData);
}
