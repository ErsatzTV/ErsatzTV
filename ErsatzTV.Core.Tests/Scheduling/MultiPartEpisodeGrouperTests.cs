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
        [TestCase("Episode 1", "Episode 2 (1)", "Episode 3 (2)", "Episode 4")]
        [TestCase("Episode 1 - More", "Episode 2 (1) - Title", "Episode 3 (2) - After", "Episode 4 - Dash")]
        [TestCase("Episode 1", "Episode 2 Part 1", "Episode 3 Part 2", "Episode 4")]
        public void NotGrouped_Grouped_NotGrouped(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one),
                NamedEpisode(two),
                NamedEpisode(three),
                NamedEpisode(four)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems);

            result.Count.Should().Be(3);
            result[0].First.Should().Be(mediaItems[0]);
            result[1].First.Should().Be(mediaItems[1]);
            result[1].Additional[0].Should().Be(mediaItems[2]);
            result[2].First.Should().Be(mediaItems[3]);
        }

        [Test]
        [TestCase("Episode 1 (1)", "Episode 2 (2)", "Episode 3")]
        [TestCase("Episode 1 (1) - More", "Episode 2 (2) - Title", "Episode 3 - After")]
        [TestCase("Episode 1 Part 1", "Episode 2 Part 2", "Episode 3")]
        public void Grouped_NotGrouped(string one, string two, string three)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one),
                NamedEpisode(two),
                NamedEpisode(three)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems);

            result.Count.Should().Be(2);
            result[0].First.Should().Be(mediaItems[0]);
            result[0].Additional[0].Should().Be(mediaItems[1]);
            result[1].First.Should().Be(mediaItems[2]);
        }

        [Test]
        [TestCase("Episode 1 (1)", "Episode 2 (2)", "Episode 3", "Episode 4 (1)", "Episode 5 (2)")]
        [TestCase(
            "Episode 1 (1) - More",
            "Episode 2 (2) - Title",
            "Episode 3 - After",
            "Episode 4 (1) - Dash",
            "Episode 5 (2) - Again")]
        [TestCase("Episode 1 Part 1", "Episode 2 Part 2", "Episode 3", "Episode 4 Part 1", "Episode 5 Part 2")]
        public void Grouped_NotGrouped_Grouped(string one, string two, string three, string four, string five)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one),
                NamedEpisode(two),
                NamedEpisode(three),
                NamedEpisode(four),
                NamedEpisode(five)
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
        [TestCase("Episode 1 (1)", "Episode 2 (2)", "Episode 3 (1)", "Episode 4 (2)")]
        [TestCase("Episode 1 (1) - More", "Episode 2 (2) - Title", "Episode 3 (1) - After", "Episode 4 (2) - Dash")]
        [TestCase("Episode 1 Part 1", "Episode 2 Part 2", "Episode 3 Part 1", "Episode 4 Part 2")]
        public void Grouped_Grouped(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one),
                NamedEpisode(two),
                NamedEpisode(three),
                NamedEpisode(four)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems);

            result.Count.Should().Be(2);
            result[0].First.Should().Be(mediaItems[0]);
            result[0].Additional[0].Should().Be(mediaItems[1]);
            result[1].First.Should().Be(mediaItems[2]);
            result[1].Additional[0].Should().Be(mediaItems[3]);
        }

        [Test]
        [TestCase("Episode 1", "Episode 2 (2)", "Episode 3 (1)", "Episode 4 (2)")]
        [TestCase("Episode 1 - More", "Episode 2 (2) - Title", "Episode 3 (1) - After", "Episode 4 (2) - Dash")]
        [TestCase("Episode 1", "Episode 2 Part 2", "Episode 3 Part 1", "Episode 4 Part 2")]
        public void Part2_Without_Part1(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one),
                NamedEpisode(two),
                NamedEpisode(three),
                NamedEpisode(four)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems);

            result.Count.Should().Be(3);
            result[0].First.Should().Be(mediaItems[0]);
            result[1].First.Should().Be(mediaItems[1]);
            result[2].First.Should().Be(mediaItems[2]);
            result[2].Additional[0].Should().Be(mediaItems[3]);
        }

        [Test]
        [TestCase("Episode 1 (1)", "Episode 3 (3)", "Episode 4", "Episode 5")]
        [TestCase("Episode 1 (1) - More", "Episode 3 (3) - Title", "Episode 4 - After", "Episode 5 - Dash")]
        [TestCase("Episode 1 Part 1", "Episode 3 Part 3", "Episode 4", "Episode 5")]
        public void Skip_Part(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one),
                NamedEpisode(two),
                NamedEpisode(three),
                NamedEpisode(four)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems);

            result.Count.Should().Be(4);
            result[0].First.Should().Be(mediaItems[0]);
            result[1].First.Should().Be(mediaItems[1]);
            result[2].First.Should().Be(mediaItems[2]);
            result[3].First.Should().Be(mediaItems[3]);
        }

        [Test]
        [TestCase("Episode 1 (1)", "Episode 3 (1)", "Episode 4 (2)", "Episode 5")]
        [TestCase("Episode 1 (1) - More", "Episode 3 (1) - Title", "Episode 4 (2) - After", "Episode 5 - Dash")]
        [TestCase("Episode 1 Part 1", "Episode 3 Part 1", "Episode 4 Part 2", "Episode 5")]
        public void Repeat_Part(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one),
                NamedEpisode(two),
                NamedEpisode(three),
                NamedEpisode(four)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems);

            result.Count.Should().Be(3);
            result[0].First.Should().Be(mediaItems[0]);
            result[1].First.Should().Be(mediaItems[1]);
            result[1].Additional[0].Should().Be(mediaItems[2]);
            result[2].First.Should().Be(mediaItems[3]);
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
