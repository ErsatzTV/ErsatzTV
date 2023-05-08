using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class ShuffledContentTests
{
    // this seed will produce (shuffle) 1-10 in order
    private const int MagicSeed = 670596;

    private CancellationToken _cancellationToken;
    
    [SetUp]
    public void SetUp()
    {
        _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
    }

    [Test]
    public void Episodes_Should_Not_Duplicate_When_Reshuffling()
    {
        List<MediaItem> contents = Episodes(10);

        // normally returns 10 5 7 4 3 6 2 8 9 1 1 (note duplicate 1 at end)
        var state = new CollectionEnumeratorState { Seed = 8 };

        var groupedMediaItems = contents.Map(mi => new GroupedMediaItem(mi, null)).ToList();
        var shuffledContent = new ShuffledMediaCollectionEnumerator(groupedMediaItems, state, _cancellationToken);

        var list = new List<int>();
        for (var i = 1; i <= 1000; i++)
        {
            shuffledContent.Current.IsSome.Should().BeTrue();
            shuffledContent.Current.Do(x => list.Add(x.Id));
            shuffledContent.MoveNext();
        }

        for (var i = 0; i < list.Count - 1; i++)
        {
            if (list[i] == list[i + 1])
            {
                Assert.Fail("List contains duplicate items");
            }
        }
    }

    [Test]
    [Timeout(2000)]
    public void Duplicate_Check_Should_Ignore_Single_Item()
    {
        List<MediaItem> contents = Episodes(1);

        var state = new CollectionEnumeratorState();

        var groupedMediaItems = contents.Map(mi => new GroupedMediaItem(mi, null)).ToList();
        var shuffledContent = new ShuffledMediaCollectionEnumerator(groupedMediaItems, state, _cancellationToken);

        var list = new List<int>();
        for (var i = 1; i <= 10; i++)
        {
            shuffledContent.Current.IsSome.Should().BeTrue();
            shuffledContent.Current.Do(x => list.Add(x.Id));
            shuffledContent.MoveNext();
        }

        list.Should().Equal(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
    }

    [Test]
    public void Episodes_Should_Shuffle()
    {
        List<MediaItem> contents = Episodes(10);

        var state = new CollectionEnumeratorState();

        var groupedMediaItems = contents.Map(mi => new GroupedMediaItem(mi, null)).ToList();
        var shuffledContent = new ShuffledMediaCollectionEnumerator(groupedMediaItems, state, _cancellationToken);

        var list = new List<int>();
        for (var i = 1; i <= 10; i++)
        {
            shuffledContent.Current.IsSome.Should().BeTrue();
            shuffledContent.Current.Do(x => list.Add(x.Id));
            shuffledContent.MoveNext();
        }

        list.Should().NotEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        list.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
    }

    [Test]
    public void State_Index_Should_Increment()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState();

        var groupedMediaItems = contents.Map(mi => new GroupedMediaItem(mi, null)).ToList();
        var shuffledContent = new ShuffledMediaCollectionEnumerator(groupedMediaItems, state, _cancellationToken);

        for (var i = 0; i < 10; i++)
        {
            shuffledContent.State.Index.Should().Be(i);
            shuffledContent.MoveNext();
        }
    }

    [Test]
    public void State_Should_Impact_Iterator_Start()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState { Index = 5, Seed = MagicSeed };

        var groupedMediaItems = contents.Map(mi => new GroupedMediaItem(mi, null)).ToList();
        var shuffledContent = new ShuffledMediaCollectionEnumerator(groupedMediaItems, state, _cancellationToken);

        for (var i = 6; i <= 10; i++)
        {
            shuffledContent.Current.IsSome.Should().BeTrue();
            shuffledContent.Current.Map(x => x.Id).IfNone(-1).Should().Be(i);
            shuffledContent.State.Index.Should().Be(i - 1);
            shuffledContent.MoveNext();
        }
    }

    [Test]
    [Timeout(1000)]
    public void State_Should_Reset_When_Invalid()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState { Index = 10, Seed = MagicSeed };

        var groupedMediaItems = contents.Map(mi => new GroupedMediaItem(mi, null)).ToList();
        var shuffledContent = new ShuffledMediaCollectionEnumerator(groupedMediaItems, state, _cancellationToken);

        shuffledContent.State.Index.Should().Be(0);
        shuffledContent.State.Seed.Should().NotBe(MagicSeed);
    }

    private static List<MediaItem> Episodes(int count) =>
        Range(1, count).Map(
                i => (MediaItem)new Episode
                {
                    Id = i,
                    EpisodeMetadata = new List<EpisodeMetadata>
                    {
                        new()
                        {
                            ReleaseDate = new DateTime(2020, 1, i)
                        }
                    }
                })
            .Reverse()
            .ToList();
}
