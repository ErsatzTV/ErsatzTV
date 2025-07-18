using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class PlaylistEnumeratorTests
{
    [Test]
    public async Task Test_PlayAll_Before_Last_PlaylistItem()
    {
        // test a 1 item, b 2 items play all, c 2 items
        // a1, b1, b2, c1, a1, b1, b2, c2

        // this isn't needed for chronological, so no need to implement anything
        IMediaCollectionRepository repo = Substitute.For<IMediaCollectionRepository>();

        var playlistItemMap = new Dictionary<PlaylistItem, List<MediaItem>>
        {
            {
                new PlaylistItem
                {
                    Id = 1,
                    PlaybackOrder = PlaybackOrder.Chronological,
                    PlayAll = false,
                    CollectionType = ProgramScheduleItemCollectionType.Collection,
                    CollectionId = 1
                },
                [FakeMovie(10)]
            },
            {
                new PlaylistItem
                {
                    Id = 2,
                    PlaybackOrder = PlaybackOrder.Chronological,
                    PlayAll = true,
                    CollectionType = ProgramScheduleItemCollectionType.Collection,
                    CollectionId = 2
                },
                [FakeMovie(20), FakeMovie(21)]
            },
            {
                new PlaylistItem
                {
                    Id = 3,
                    PlaybackOrder = PlaybackOrder.Chronological,
                    PlayAll = false,
                    CollectionType = ProgramScheduleItemCollectionType.Collection,
                    CollectionId = 3
                },
                [FakeMovie(30), FakeMovie(31)]
            }
        };

        PlaylistEnumerator enumerator = await PlaylistEnumerator.Create(
            repo,
            playlistItemMap,
            new CollectionEnumeratorState(),
            false,
            CancellationToken.None);

        var items = new List<int>();
        items.AddRange(enumerator.Current.Map(mi => mi.Id));

        enumerator.MoveNext();
        while (enumerator.State.Index > 0)
        {
            items.AddRange(enumerator.Current.Map(mi => mi.Id));
            enumerator.MoveNext();
        }

        items.Count.ShouldBe(8);
        items.ShouldBe([10, 20, 21, 30, 10, 20, 21, 31]);
    }

    [Test]
    public async Task Test_PlayAll_Last_PlaylistItem()
    {
        // test a 1 item, b 2 items, c 2 items play all
        // a1, b1, c1, c2, a1, b2, c1, c2

        // this isn't needed for chronological, so no need to implement anything
        IMediaCollectionRepository repo = Substitute.For<IMediaCollectionRepository>();

        var playlistItemMap = new Dictionary<PlaylistItem, List<MediaItem>>
        {
            {
                new PlaylistItem
                {
                    Id = 1,
                    PlaybackOrder = PlaybackOrder.Chronological,
                    PlayAll = false,
                    CollectionType = ProgramScheduleItemCollectionType.Collection,
                    CollectionId = 1
                },
                [FakeMovie(10)]
            },
            {
                new PlaylistItem
                {
                    Id = 2,
                    PlaybackOrder = PlaybackOrder.Chronological,
                    PlayAll = false,
                    CollectionType = ProgramScheduleItemCollectionType.Collection,
                    CollectionId = 2
                },
                [FakeMovie(20), FakeMovie(21)]
            },
            {
                new PlaylistItem
                {
                    Id = 3,
                    PlaybackOrder = PlaybackOrder.Chronological,
                    PlayAll = true,
                    CollectionType = ProgramScheduleItemCollectionType.Collection,
                    CollectionId = 3
                },
                [FakeMovie(30), FakeMovie(31)]
            }
        };

        PlaylistEnumerator enumerator = await PlaylistEnumerator.Create(
            repo,
            playlistItemMap,
            new CollectionEnumeratorState(),
            false,
            CancellationToken.None);

        var items = new List<int>();
        items.AddRange(enumerator.Current.Map(mi => mi.Id));

        enumerator.MoveNext();
        while (enumerator.State.Index > 0)
        {
            items.AddRange(enumerator.Current.Map(mi => mi.Id));
            enumerator.MoveNext();
        }

        items.Count.ShouldBe(8);
        items.ShouldBe([10, 20, 30, 31, 10, 21, 30, 31]);
    }

    private static Movie FakeMovie(int id) => new()
    {
        Id = id,
        MediaVersions = [],
        MovieMetadata =
        [
            new MovieMetadata
            {
                ReleaseDate = new DateTime(2020, 1, id)
            }
        ]
    };
}
