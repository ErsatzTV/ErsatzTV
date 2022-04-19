using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using LanguageExt.UnsafeValueAccess;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class ShuffledMediaCollectionEnumeratorTests
{
    private readonly List<GroupedMediaItem> _mediaItems = new()
    {
        new GroupedMediaItem(new MediaItem { Id = 1 }, new List<MediaItem>()),
        new GroupedMediaItem(new MediaItem { Id = 2 }, new List<MediaItem>()),
        new GroupedMediaItem(new MediaItem { Id = 3 }, new List<MediaItem>())
    };

    [Test]
    public void Peek_Zero_Should_Match_Current()
    {
        var state = new CollectionEnumeratorState { Index = 0, Seed = 0 };
        var enumerator = new ShuffledMediaCollectionEnumerator(_mediaItems, state);

        Option<MediaItem> peek = enumerator.Peek(0);
        Option<MediaItem> current = enumerator.Current;

        peek.IsSome.Should().BeTrue();
        current.IsSome.Should().BeTrue();
        peek.ValueUnsafe().Id.Should().Be(1);
        current.ValueUnsafe().Id.Should().Be(1);
    }

    [Test]
    public void Peek_One_Should_Match_Next()
    {
        var state = new CollectionEnumeratorState { Index = 0, Seed = 0 };
        var enumerator = new ShuffledMediaCollectionEnumerator(_mediaItems, state);

        Option<MediaItem> peek = enumerator.Peek(1);

        enumerator.MoveNext();
        Option<MediaItem> next = enumerator.Current;

        peek.IsSome.Should().BeTrue();
        next.IsSome.Should().BeTrue();
        peek.ValueUnsafe().Id.Should().Be(2);
        next.ValueUnsafe().Id.Should().Be(2);
    }

    [Test]
    public void Peek_Two_Should_Match_NextNext()
    {
        var state = new CollectionEnumeratorState { Index = 0, Seed = 0 };
        var enumerator = new ShuffledMediaCollectionEnumerator(_mediaItems, state);

        Option<MediaItem> peek = enumerator.Peek(2);

        enumerator.MoveNext();
        enumerator.MoveNext();
        Option<MediaItem> next = enumerator.Current;

        peek.IsSome.Should().BeTrue();
        next.IsSome.Should().BeTrue();
        peek.ValueUnsafe().Id.Should().Be(3);
        next.ValueUnsafe().Id.Should().Be(3);
    }

    [Test]
    public void Peek_Three_Should_Match_NextNextNext()
    {
        var state = new CollectionEnumeratorState { Index = 0, Seed = 0 };
        var enumerator = new ShuffledMediaCollectionEnumerator(_mediaItems, state);

        Option<MediaItem> peek = enumerator.Peek(3);

        enumerator.MoveNext();
        enumerator.MoveNext();
        enumerator.MoveNext();
        Option<MediaItem> next = enumerator.Current;

        peek.IsSome.Should().BeTrue();
        next.IsSome.Should().BeTrue();
        peek.ValueUnsafe().Id.Should().Be(2);
        next.ValueUnsafe().Id.Should().Be(2);
    }
}
