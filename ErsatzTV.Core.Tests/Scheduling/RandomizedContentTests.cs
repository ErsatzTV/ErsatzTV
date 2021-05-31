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
    public class RandomizedContentTests
    {
        private const int KnownSeed = 22295;

        private readonly List<int> _expected = new()
        {
            5, 7, 7, 8, 6, 7, 8, 9, 10, 7, 5, 1, 7, 2, 5, 6, 1, 4, 5, 6, 4, 5, 1, 6, 5, 7, 1, 3, 9, 9, 9, 3,
            3, 2, 3, 4, 5, 6, 9, 3, 6, 9, 7, 1, 2, 10, 3, 8, 3, 8, 8, 3, 1, 5, 4, 3, 6, 4, 6, 2, 9, 8, 3, 1, 8, 5,
            1, 8, 2, 1, 1, 5, 5, 5, 3, 5, 8, 10, 4, 8, 7, 3, 3, 4, 4, 9, 2, 8, 8, 10, 8, 4, 3, 10, 7, 8, 9, 9
        };

        [Test]
        public void Episodes_Should_Randomize()
        {
            List<MediaItem> contents = Episodes(10);

            var state = new CollectionEnumeratorState();

            var randomizedContent = new RandomizedMediaCollectionEnumerator(contents, state);

            var list = new List<int>();
            for (var i = 1; i <= 10; i++)
            {
                randomizedContent.Current.IsSome.Should().BeTrue();
                randomizedContent.Current.Do(c => list.Add(c.Id));

                randomizedContent.MoveNext();
            }

            list.Should().NotEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            list.Should().NotEqual(new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 });
        }

        [Test]
        public void State_Index_Should_Increment()
        {
            List<MediaItem> contents = Episodes(10);
            var state = new CollectionEnumeratorState();

            var randomizedContent = new RandomizedMediaCollectionEnumerator(contents, state);

            for (var i = 1; i <= 10; i++)
            {
                randomizedContent.State.Index.Should().Be(i);

                randomizedContent.MoveNext();
            }
        }

        [Test]
        public void State_Should_Impact_Iterator_Start()
        {
            List<MediaItem> contents = Episodes(10);
            var state = new CollectionEnumeratorState { Index = 5, Seed = KnownSeed };

            var randomizedContent = new RandomizedMediaCollectionEnumerator(contents, state);

            for (var i = 6; i <= 99; i++)
            {
                randomizedContent.Current.IsSome.Should().BeTrue();
                // this test data setup/expectation is confusing
                randomizedContent.Current.Map(c => c.Id).IfNone(-1).Should().Be(_expected[i - 2]);
                randomizedContent.State.Index.Should().Be(i);

                randomizedContent.MoveNext();
            }
        }
        
        [Test]
        [Timeout(1000)]
        public void State_Index_Should_Continue_Past_End_Of_Items()
        {
            List<MediaItem> contents = Episodes(10);
            var state = new CollectionEnumeratorState { Index = 10, Seed = KnownSeed };

            var _ = new RandomizedMediaCollectionEnumerator(contents, state);
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
