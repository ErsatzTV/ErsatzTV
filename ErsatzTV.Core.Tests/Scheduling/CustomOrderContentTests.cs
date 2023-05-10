using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling;

public class CustomOrderContentTests
{
    private CancellationToken _cancellationToken;

    [SetUp]
    public void SetUp() => _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    [Test]
    public void MediaItems_Should_Sort_By_CustomOrder()
    {
        Collection collection = CreateCollection(10);
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState();

        var customOrderContent = new CustomOrderCollectionEnumerator(collection, contents, state);

        for (var i = 10; i >= 1; i--)
        {
            customOrderContent.Current.IsSome.Should().BeTrue();
            customOrderContent.Current.Map(x => x.Id).IfNone(-1).Should().Be(i);
            customOrderContent.MoveNext();
        }
    }

    [Test]
    public void State_Index_Should_Increment()
    {
        Collection collection = CreateCollection(10);
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState();

        var customOrderContent = new CustomOrderCollectionEnumerator(collection, contents, state);

        for (var i = 0; i < 10; i++)
        {
            customOrderContent.State.Index.Should().Be(i % 10);
            customOrderContent.MoveNext();
        }
    }

    [Test]
    public void State_Should_Impact_Iterator_Start()
    {
        Collection collection = CreateCollection(10);
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState { Index = 5 };

        var customOrderContent = new CustomOrderCollectionEnumerator(collection, contents, state);

        for (var i = 5; i >= 1; i--)
        {
            customOrderContent.Current.IsSome.Should().BeTrue();
            customOrderContent.Current.Map(x => x.Id).IfNone(-1).Should().Be(i);
            customOrderContent.State.Index.Should().Be(5 - i + 5); // 5 through 10
            customOrderContent.MoveNext();
        }
    }

    private static Collection CreateCollection(int episodeCount)
    {
        var collection = new Collection { CollectionItems = new List<CollectionItem>() };

        for (var i = 1; i <= episodeCount; i++)
        {
            collection.CollectionItems.Add(
                new CollectionItem
                {
                    MediaItemId = i,
                    // reverse order
                    CustomIndex = episodeCount - i
                });
        }

        return collection;
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
