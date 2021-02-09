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
    public class ShuffledContentTests
    {
        // this seed will produce (shuffle) 1-10 in order
        private const int MagicSeed = 670596;

        [Test]
        public void Episodes_Should_Shuffle()
        {
            List<MediaItem> contents = Episodes(10);

            var state = new MediaCollectionEnumeratorState();

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
            var state = new MediaCollectionEnumeratorState();

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
            var state = new MediaCollectionEnumeratorState { Index = 5, Seed = MagicSeed };

            var shuffledContent = new ShuffledMediaCollectionEnumerator(contents, state);

            for (var i = 6; i <= 10; i++)
            {
                shuffledContent.Current.IsSome.Should().BeTrue();
                shuffledContent.Current.Map(x => x.Id).IfNone(-1).Should().Be(i);
                shuffledContent.State.Index.Should().Be(i - 1);
                shuffledContent.MoveNext();
            }
        }

        [Test]
        public void Peek_Should_Not_Impact_Current_Or_Wrapping()
        {
            List<MediaItem> contents = Episodes(10);
            var state = new MediaCollectionEnumeratorState { Seed = MagicSeed };
            var expected = new List<int>
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 6, 1, 4, 2, 10, 7, 3, 5, 8, 9, 10, 9, 4, 5, 1, 7, 3, 2, 8, 6, 3, 5, 4, 2,
                10, 8, 7, 1, 6, 9, 3, 4, 7, 10, 6, 9, 1, 2, 8, 5, 8, 1, 3, 6, 5, 7, 9, 4, 2, 10, 6, 1, 4, 3, 5, 10, 2,
                7, 8, 9, 6, 10, 4, 3, 8, 1, 5, 9, 2, 7, 8, 6, 4, 1, 9, 7, 3, 10, 5, 2, 5, 9, 2, 6, 7, 10, 3, 4, 1, 8
            };

            var shuffledContent = new ShuffledMediaCollectionEnumerator(contents, state);

            for (var i = 0; i < 99; i++)
            {
                shuffledContent.Current.IsSome.Should().BeTrue();
                shuffledContent.Current.Map(x => x.Id).IfNone(-1).Should().Be(expected[i]);
                shuffledContent.Peek.Map(x => x.Id).IfNone(-1).Should().Be(expected[i + 1]);
                shuffledContent.Peek.Map(x => x.Id).IfNone(-1).Should().Be(expected[i + 1]);

                shuffledContent.MoveNext();
            }
        }

        private static List<MediaItem> Episodes(int count) =>
            Range(1, count).Map(
                    i => new MediaItem
                    {
                        Id = i,
                        Metadata = new MediaMetadata
                        {
                            MediaType = MediaType.TvShow, Aired = new DateTime(2020, 1, i)
                        }
                    })
                .Reverse()
                .ToList();
    }
}
