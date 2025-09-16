using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Tests.Fakes;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling.ClassicScheduling;

[TestFixture]
public class RefreshPlayoutTests : PlayoutBuilderTestBase
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
                CollectionType = CollectionType.Collection,
                EnumeratorState = new CollectionEnumeratorState
                {
                    Index = 1,
                    Seed = 12345
                },
                Playout = playout
            });

        var referenceData = new PlayoutReferenceData(
            playout.Channel,
            Option<Deco>.None,
            [],
            [],
            playout.ProgramSchedule,
            [],
            [],
            TimeSpan.Zero);

        IConfigElementRepository configRepo = Substitute.For<IConfigElementRepository>();
        var televisionRepo = new FakeTelevisionRepository();
        IArtistRepository artistRepo = Substitute.For<IArtistRepository>();
        IMultiEpisodeShuffleCollectionEnumeratorFactory factory =
            Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>();
        ILocalFileSystem localFileSystem = Substitute.For<ILocalFileSystem>();
        IRerunHelper rerunHelper = Substitute.For<IRerunHelper>();
        var builder = new PlayoutBuilder(
            configRepo,
            fakeRepository,
            televisionRepo,
            artistRepo,
            factory,
            localFileSystem,
            rerunHelper,
            Logger);

        DateTimeOffset start = HoursAfterMidnight(24);
        DateTimeOffset finish = start + TimeSpan.FromDays(1);

        PlayoutBuildResult result = await builder.Build(
            playout,
            referenceData,
            PlayoutBuildResult.Empty,
            PlayoutBuildMode.Refresh,
            start,
            finish,
            CancellationToken);

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
