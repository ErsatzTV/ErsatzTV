using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

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
            shufflePlaylistItems: false,
            CancellationToken.None);

        enumerator.MoveNext();
        var totalLength = 1;
        while (enumerator.State.Index > 0)
        {
            enumerator.MoveNext();
            totalLength += 1;
        }

        totalLength.Should().Be(8);
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
            shufflePlaylistItems: false,
            CancellationToken.None);

        enumerator.MoveNext();
        var totalLength = 1;
        while (enumerator.State.Index > 0)
        {
            enumerator.MoveNext();
            totalLength += 1;
        }

        totalLength.Should().Be(8);
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
