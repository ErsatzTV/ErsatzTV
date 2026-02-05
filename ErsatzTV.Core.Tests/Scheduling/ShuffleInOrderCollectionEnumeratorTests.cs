using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using LanguageExt.UnsafeValueAccess;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class ShuffleInOrderCollectionEnumeratorTests
{
    [Test]
    public void Should_Not_Repeat_Items_Until_Cycle_Complete()
    {
        var collections = new List<CollectionWithItems>
        {
            new(
                0,
                0,
                "1",
                Enumerable.Range(1, 10).Select(i => new Movie { Id = i, MovieMetadata = [] }).Cast<MediaItem>().ToList(),
                true,
                PlaybackOrder.ShuffleInOrder,
                false),
            new(
                0,
                0,
                "2",
                Enumerable.Range(11, 20).Select(i => new Movie { Id = i, MovieMetadata = [] }).Cast<MediaItem>().ToList(),
                true,
                PlaybackOrder.ShuffleInOrder,
                false)
        };

        var state = new CollectionEnumeratorState { Seed = 1234, Index = 0 };
        var enumerator = new ShuffleInOrderCollectionEnumerator(collections, state, false, CancellationToken.None);

        var seenIds = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < 20; i++)
        {
            enumerator.Current.IsSome.ShouldBeTrue();
            int id = enumerator.Current.ValueUnsafe().Id;
            seenIds.ShouldNotContain(id, $"at index {i}");
            seenIds.Add(id);
            enumerator.MoveNext(Option<DateTimeOffset>.None);
        }

        seenIds.Count.ShouldBe(20);
    }

    [Test]
    public void Should_Handle_Single_Collection()
    {
        var collections = new List<CollectionWithItems>
        {
            new(
                0,
                0,
                "1",
                Enumerable.Range(1, 10).Select(i => new Movie { Id = i, MovieMetadata = [] }).Cast<MediaItem>().ToList(),
                true,
                PlaybackOrder.ShuffleInOrder,
                false)
        };

        var state = new CollectionEnumeratorState { Seed = 1234, Index = 0 };
        var enumerator = new ShuffleInOrderCollectionEnumerator(collections, state, false, CancellationToken.None);

        var seenIds = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            seenIds.Add(enumerator.Current.ValueUnsafe().Id);
            enumerator.MoveNext(Option<DateTimeOffset>.None);
        }

        seenIds.Count.ShouldBe(10);
        seenIds.ShouldBeInOrder(SortDirection.Ascending);
    }

    [Test]
    public void Should_Reshuffle_After_Cycle()
    {
         var collections = new List<CollectionWithItems>
        {
            new(
                0,
                0,
                "1",
                Enumerable.Range(1, 10).Select(i => new Movie { Id = i, MovieMetadata = [] }).Cast<MediaItem>().ToList(),
                true,
                PlaybackOrder.ShuffleInOrder,
                false)
        };

        var state = new CollectionEnumeratorState { Seed = 1234, Index = 0 };
        var enumerator = new ShuffleInOrderCollectionEnumerator(collections, state, false, CancellationToken.None);

        for (int i = 0; i < 10; i++)
        {
            enumerator.MoveNext(Option<DateTimeOffset>.None);
        }

        enumerator.State.Index.ShouldBe(0);
        // Should have a new seed
        enumerator.State.Seed.ShouldNotBe(1234);
    }

    [Test]
    public void ResetState_Should_Update_Seed()
    {
        var collections = new List<CollectionWithItems>
        {
            new(
                0,
                0,
                "1",
                Enumerable.Range(1, 10).Select(i => new Movie { Id = i, MovieMetadata = [] }).Cast<MediaItem>().ToList(),
                true,
                PlaybackOrder.ShuffleInOrder,
                false)
        };

        var state = new CollectionEnumeratorState { Seed = 1234, Index = 0 };
        var enumerator = new ShuffleInOrderCollectionEnumerator(collections, state, false, CancellationToken.None);

        var newState = new CollectionEnumeratorState { Seed = 5678, Index = 5 };
        enumerator.ResetState(newState);

        enumerator.State.Seed.ShouldBe(5678);
        enumerator.State.Index.ShouldBe(5);
    }
}
