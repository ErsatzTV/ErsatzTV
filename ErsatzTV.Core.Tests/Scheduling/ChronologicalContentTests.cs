using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using Shouldly;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class ChronologicalContentTests
{
    [SetUp]
    public void SetUp() => _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    private CancellationToken _cancellationToken;

    [Test]
    public void Episodes_Should_Sort_By_Aired()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState();

        var chronologicalContent = new ChronologicalMediaCollectionEnumerator(contents, state);

        for (var i = 1; i <= 10; i++)
        {
            chronologicalContent.Current.IsSome.ShouldBeTrue();
            chronologicalContent.Current.Map(x => x.Id).IfNone(-1).ShouldBe(i);
            chronologicalContent.MoveNext();
        }
    }

    [Test]
    public void State_Index_Should_Increment()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState();

        var chronologicalContent = new ChronologicalMediaCollectionEnumerator(contents, state);

        for (var i = 0; i < 10; i++)
        {
            chronologicalContent.State.Index.ShouldBe(i % 10);
            chronologicalContent.MoveNext();
        }
    }

    [Test]
    public void State_Should_Impact_Iterator_Start()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState { Index = 5 };

        var chronologicalContent = new ChronologicalMediaCollectionEnumerator(contents, state);

        for (var i = 6; i <= 10; i++)
        {
            chronologicalContent.Current.IsSome.ShouldBeTrue();
            chronologicalContent.Current.Map(x => x.Id).IfNone(-1).ShouldBe(i);
            chronologicalContent.State.Index.ShouldBe(i - 1);
            chronologicalContent.MoveNext();
        }
    }

    [Test]
    public void State_Should_Reset_When_Invalid()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState { Index = 10 };

        var chronologicalContent = new ChronologicalMediaCollectionEnumerator(contents, state);

        chronologicalContent.State.Index.ShouldBe(0);
        chronologicalContent.State.Seed.ShouldBe(0);
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
                            ReleaseDate = new DateTime(2020, 1, i),
                            EpisodeNumber = 20 - i
                        }
                    }
                })
            .Reverse()
            .ToList();
}
