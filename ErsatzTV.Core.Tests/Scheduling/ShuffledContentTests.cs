using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using NUnit.Framework;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Tests.Scheduling
{
    [TestFixture]
    public class ShuffledContentTests
    {
        // this seed will produce (shuffle) 1-10 in order
        private const int MagicSeed = 670596;

        [Test]
        public void Episodes_Should_Shuffle()
        {
            List<MediaItem> contents = Episodes(10);

            var state = new CollectionEnumeratorState();

            var shuffledContent = new ShuffledMediaCollectionEnumerator(contents, state);

            var list = new List<int>();
            for (var i = 1; i <= 10; i++)
            {
                shuffledContent.Current.IsSome.Should().BeTrue();
                shuffledContent.Current.Do(x => list.Add(x.Id));
                shuffledContent.MoveNext();
            }

            list.Should().NotEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            list.Should().BeEquivalentTo(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Test]
        public void State_Index_Should_Increment()
        {
            List<MediaItem> contents = Episodes(10);
            var state = new CollectionEnumeratorState();

            var shuffledContent = new ShuffledMediaCollectionEnumerator(contents, state);

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

            var shuffledContent = new ShuffledMediaCollectionEnumerator(contents, state);

            for (var i = 6; i <= 10; i++)
            {
                shuffledContent.Current.IsSome.Should().BeTrue();
                shuffledContent.Current.Map(x => x.Id).IfNone(-1).Should().Be(i);
                shuffledContent.State.Index.Should().Be(i - 1);
                shuffledContent.MoveNext();
            }
        }

        private static List<MediaItem> Episodes(int count) =>
            Range(1, count).Map(
                    i => (MediaItem) new Episode
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
}
