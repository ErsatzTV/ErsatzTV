using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling
{
    public class MultiPartEpisodeGrouperTests
    {
        [Test]
        public void NotGrouped_Grouped_NotGrouped()
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode("Episode 1"),
                NamedEpisode("Episode 2 (1)"),
                NamedEpisode("Episode 3 (2)"),
                NamedEpisode("Episode 4"),
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems);

            result.Count.Should().Be(3);
            result[0].First.Should().Be(mediaItems[0]);
            result[1].First.Should().Be(mediaItems[1]);
            result[1].Additional[0].Should().Be(mediaItems[2]);
            result[2].First.Should().Be(mediaItems[3]);
        }

        [Test]
        public void Grouped_NotGrouped()
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode("Episode 1 (1)"),
                NamedEpisode("Episode 2 (2)"),
                NamedEpisode("Episode 3"),
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems);

            result.Count.Should().Be(2);
            result[0].First.Should().Be(mediaItems[0]);
            result[0].Additional[0].Should().Be(mediaItems[1]);
            result[1].First.Should().Be(mediaItems[2]);
        }

        [Test]
        public void Grouped_NotGrouped_Grouped()
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode("Episode 1 (1)"),
                NamedEpisode("Episode 2 (2)"),
                NamedEpisode("Episode 3"),
                NamedEpisode("Episode 4 (1)"),
                NamedEpisode("Episode 5 (2)"),
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems);

            result.Count.Should().Be(3);
            result[0].First.Should().Be(mediaItems[0]);
            result[0].Additional[0].Should().Be(mediaItems[1]);
            result[1].First.Should().Be(mediaItems[2]);
            result[2].First.Should().Be(mediaItems[3]);
            result[2].Additional[0].Should().Be(mediaItems[4]);
        }

        [Test]
        public void Grouped_Grouped()
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode("Episode 1 (1)"),
                NamedEpisode("Episode 2 (2)"),
                NamedEpisode("Episode 3 (1)"),
                NamedEpisode("Episode 4 (2)"),
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems);

            result.Count.Should().Be(2);
            result[0].First.Should().Be(mediaItems[0]);
            result[0].Additional[0].Should().Be(mediaItems[1]);
            result[1].First.Should().Be(mediaItems[2]);
            result[1].Additional[0].Should().Be(mediaItems[3]);
        }

        private static Episode NamedEpisode(string title) =>
            new()
            {
                EpisodeMetadata = new List<EpisodeMetadata>
                {
                    new() { Title = title }
                }
            };
    }
}
