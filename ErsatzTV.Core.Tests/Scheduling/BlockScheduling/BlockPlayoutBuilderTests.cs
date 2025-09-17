using Destructurama;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Scheduling.BlockScheduling;
using ErsatzTV.Core.Tests.Fakes;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling.BlockScheduling;

public class BlockPlayoutBuilderTests
{
    [SetUp]
    public void SetUp() => _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    private CancellationToken _cancellationToken;
    private readonly ILogger<BlockPlayoutBuilder> _logger;

    public BlockPlayoutBuilderTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .Destructure.UsingAttributes()
            .CreateLogger();

        ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

        _logger = loggerFactory.CreateLogger<BlockPlayoutBuilder>();
    }

    [TestFixture]
    public class Build : BlockPlayoutBuilderTests
    {
        [Test]
        public async Task Should_Start_At_Beginning_Of_Current_Block()
        {
            var collection = new SmartCollection
            {
                Id = 1,
                Query = "asdf"
            };

            var block = new Block
            {
                Id = 1,
                Name = "Test Block",
                Minutes = 30,
                Items =
                [
                    new BlockItem
                    {
                        CollectionType = CollectionType.SmartCollection,
                        PlaybackOrder = PlaybackOrder.Chronological,
                        Index = 1,
                        SmartCollection = collection,
                        SmartCollectionId = collection.Id
                    }
                ],
                StopScheduling = BlockStopScheduling.AfterDurationEnd
            };

            var template = new Template
            {
                Id = 1,
                Items = []
            };

            var templateItem = new TemplateItem
            {
                Block = block,
                BlockId = block.Id,
                StartTime = TimeSpan.FromHours(9),
                Template = template,
                TemplateId = template.Id
            };

            template.Items.Add(templateItem);

            var playoutTemplate = new PlayoutTemplate
            {
                Id = 1,
                Index = 1,
                Template = template,
                TemplateId = template.Id,
                DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                MonthsOfYear = PlayoutTemplate.AllMonthsOfYear()
            };

            var playout = new Playout
            {
                Id = 1,
                Channel = new Channel(Guid.Empty) { Id = 1, Name = "Test Channel" },
                Templates =
                [
                    playoutTemplate
                ],
                Items = [],
                PlayoutHistory = []
            };

            var now = new DateTimeOffset(2024, 1, 10, 9, 15, 0, TimeSpan.FromHours(-6));

            var mediaItems = new List<MediaItem>
            {
                new Movie
                {
                    Id = 1,
                    MovieMetadata = [new MovieMetadata { ReleaseDate = DateTime.Today }],
                    MediaVersions =
                    [
                        new MediaVersion
                        {
                            Duration = TimeSpan.FromMinutes(25),
                            MediaFiles = [new MediaFile { Path = "/fake/path/1" }]
                        }
                    ]
                }
            };

            var collectionRepo = new FakeMediaCollectionRepository(
                Map((collection.Id, mediaItems))
            );

            IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
            configRepo
                .GetValue<int>(Arg.Is(ConfigElementKey.PlayoutDaysToBuild), Arg.Any<CancellationToken>())
                .Returns(Some(1));

            var builder = new BlockPlayoutBuilder(
                configRepo,
                collectionRepo,
                Substitute.For<ITelevisionRepository>(),
                Substitute.For<IArtistRepository>(),
                Substitute.For<ICollectionEtag>(),
                _logger);

            var referenceData = new PlayoutReferenceData(
                playout.Channel,
                Option<Deco>.None,
                [],
                playout.Templates.ToList(),
                null,
                [],
                [],
                TimeSpan.Zero);

            PlayoutBuildResult result = await builder.Build(
                now,
                playout,
                referenceData,
                PlayoutBuildMode.Reset,
                _cancellationToken);

            // this test only cares about "today"
            result.AddedItems.RemoveAll(i => i.StartOffset.Date > now.Date);

            result.AddedItems.Count.ShouldBe(1);
            result.AddedItems[0].StartOffset.TimeOfDay.ShouldBe(TimeSpan.FromHours(9));
        }
    }
}
