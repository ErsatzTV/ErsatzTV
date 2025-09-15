using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class SeasonEpisodeContentTests
{
    [SetUp]
    public void SetUp() => _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    private CancellationToken _cancellationToken;

    [Test]
    public void Episodes_Should_Sort_By_EpisodeNumber()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState();

        var chronologicalContent = new SeasonEpisodeMediaCollectionEnumerator(contents, state);

        for (var i = 1; i <= 10; i++)
        {
            chronologicalContent.Current.IsSome.ShouldBeTrue();
            chronologicalContent.Current.Map(x => x.Id).IfNone(-1).ShouldBe(i);
            chronologicalContent.MoveNext(Option<DateTimeOffset>.None);
        }
    }

    [Test]
    public void State_Index_Should_Increment()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState();

        var chronologicalContent = new SeasonEpisodeMediaCollectionEnumerator(contents, state);

        for (var i = 0; i < 10; i++)
        {
            chronologicalContent.State.Index.ShouldBe(i % 10);
            chronologicalContent.MoveNext(Option<DateTimeOffset>.None);
        }
    }

    [Test]
    public void State_Should_Impact_Iterator_Start()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState { Index = 5 };

        var chronologicalContent = new SeasonEpisodeMediaCollectionEnumerator(contents, state);

        for (var i = 6; i <= 10; i++)
        {
            chronologicalContent.Current.IsSome.ShouldBeTrue();
            chronologicalContent.Current.Map(x => x.Id).IfNone(-1).ShouldBe(i);
            chronologicalContent.State.Index.ShouldBe(i - 1);
            chronologicalContent.MoveNext(Option<DateTimeOffset>.None);
        }
    }

    [Test]
    public void State_Should_Reset_When_Invalid()
    {
        List<MediaItem> contents = Episodes(10);
        var state = new CollectionEnumeratorState { Index = 10 };

        var chronologicalContent = new SeasonEpisodeMediaCollectionEnumerator(contents, state);

        chronologicalContent.State.Index.ShouldBe(0);
        chronologicalContent.State.Seed.ShouldBe(0);
    }

    [Test]
    public void Episodes_Should_Ignore_Specials()
    {
        List<MediaItem> contents = Episodes(10);
        for (int i = 0; i < 2; i++)
        {
            ((Episode)contents[i]).Season = new Season { SeasonNumber = 0 };
        }

        var state = new CollectionEnumeratorState();

        var chronologicalContent = new SeasonEpisodeMediaCollectionEnumerator(contents, state);

        for (var i = 0; i < 16; i++)
        {
            chronologicalContent.State.Index.ShouldBe(i % 8);
            chronologicalContent.MoveNext(Option<DateTimeOffset>.None);
        }
    }

    private static List<MediaItem> Episodes(int count) =>
        Range(1, count).Map(i => (MediaItem)new Episode
            {
                Id = i,
                EpisodeMetadata =
                [
                    new EpisodeMetadata
                    {
                        ReleaseDate = new DateTime(2020, 1, 20 - i),
                        EpisodeNumber = i
                    }
                ],
                Season = new Season
                {
                    SeasonNumber = 1
                }
            })
            .Reverse()
            .ToList();
}
