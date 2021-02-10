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
    public class ChronologicalContentTests
    {
        [Test]
        public void Episodes_Should_Sort_By_Aired()
        {
            List<MediaItem> contents = Episodes(10);
            var state = new MediaCollectionEnumeratorState();

            var chronologicalContent = new ChronologicalMediaCollectionEnumerator(contents, state);

            for (var i = 1; i <= 10; i++)
            {
                chronologicalContent.Current.IsSome.Should().BeTrue();
                chronologicalContent.Current.Map(x => x.Id).IfNone(-1).Should().Be(i);
                chronologicalContent.MoveNext();
            }
        }

        [Test]
        public void State_Index_Should_Increment()
        {
            List<MediaItem> contents = Episodes(10);
            var state = new MediaCollectionEnumeratorState();

            var chronologicalContent = new ChronologicalMediaCollectionEnumerator(contents, state);

            for (var i = 0; i < 10; i++)
            {
                chronologicalContent.State.Index.Should().Be(i % 10);
                chronologicalContent.MoveNext();
            }
        }

        [Test]
        public void State_Should_Impact_Iterator_Start()
        {
            List<MediaItem> contents = Episodes(10);
            var state = new MediaCollectionEnumeratorState { Index = 5 };

            var chronologicalContent = new ChronologicalMediaCollectionEnumerator(contents, state);

            for (var i = 6; i <= 10; i++)
            {
                chronologicalContent.Current.IsSome.Should().BeTrue();
                chronologicalContent.Current.Map(x => x.Id).IfNone(-1).Should().Be(i);
                chronologicalContent.State.Index.Should().Be(i - 1);
                chronologicalContent.MoveNext();
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
