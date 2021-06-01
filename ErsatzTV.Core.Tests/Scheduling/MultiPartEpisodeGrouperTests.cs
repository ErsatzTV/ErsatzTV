using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using FluentAssertions;
using NUnit.Framework;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Tests.Scheduling
{
    public class MultiPartEpisodeGrouperTests
    {
        [Test]
        [TestCase("Episode 1", "Episode 2 (1)", "Episode 3 (2)", "Episode 4")]
        [TestCase("Episode 1 - More", "Episode 2 (1) - Title", "Episode 3 (2) - After", "Episode 4 - Dash")]
        [TestCase("Episode 1", "Episode 2 Part 1", "Episode 3 Part 2", "Episode 4")]
        [TestCase("Episode 1", "Episode 2 (Part 1)", "Episode 3 (Part 2)", "Episode 4")]
        public void NotGrouped_Grouped_NotGrouped(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1),
                NamedEpisode(two, 1, 1, 2),
                NamedEpisode(three, 1, 1, 3),
                NamedEpisode(four, 1, 1, 4)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(3);
            ShouldHaveOneItem(result, mediaItems[0]);
            ShouldHaveTwoItems(result, mediaItems[1], mediaItems[2]);
            ShouldHaveOneItem(result, mediaItems[3]);
        }

        [Test]
        [TestCase("Episode 1 (1)", "Episode 2 - Part 2", "Episode 3")]
        [TestCase("Episode 1 Part 1", "Episode 2 (2) - More", "Episode 3 - After")]
        [TestCase("Episode 1 Part 1", "Episode 2 (II)", "Episode 3")]
        [TestCase("Episode 1 Part One", "Episode 2 (II)", "Episode 3")]
        [TestCase("Episode 1 (1)", "Episode 2 (Part 2)", "Episode 3")]
        public void MixedNaming_Group(string one, string two, string three)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1),
                NamedEpisode(two, 1, 1, 2),
                NamedEpisode(three, 1, 1, 3)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(2);
            ShouldHaveTwoItems(result, mediaItems[0], mediaItems[1]);
            ShouldHaveOneItem(result, mediaItems[2]);
        }

        [Test]
        [TestCase("Episode 1 (5)", "Episode 2 - (6)", "Episode 3")]
        [TestCase("Episode 1 Part 5", "Episode 2 Part 6", "Episode 3 - After")]
        [TestCase("Episode 1 Part (V)", "Episode 2 (VI)", "Episode 3")]
        [TestCase("Episode 1 (Part 5)", "Episode 2 (Part 6)", "Episode 3")]
        public void Only_Later_Parts(string one, string two, string three)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1),
                NamedEpisode(two, 1, 1, 2),
                NamedEpisode(three, 1, 1, 3)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(2);
            ShouldHaveTwoItems(result, mediaItems[0], mediaItems[1]);
            ShouldHaveOneItem(result, mediaItems[2]);
        }

        [Test]
        [TestCase("Episode 1 (1)", "Episode 2 (2)", "Episode 3")]
        [TestCase("Episode 1 (1) - More", "Episode 2 (2) - Title", "Episode 3 - After")]
        [TestCase("Episode 1 Part 1", "Episode 2 Part 2", "Episode 3")]
        [TestCase("Episode 1 (Part 1)", "Episode 2 (Part 2)", "Episode 3")]
        public void Grouped_NotGrouped(string one, string two, string three)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1),
                NamedEpisode(two, 1, 1, 2),
                NamedEpisode(three, 1, 1, 3)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(2);
            ShouldHaveTwoItems(result, mediaItems[0], mediaItems[1]);
            ShouldHaveOneItem(result, mediaItems[2]);
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
        [TestCase("Episode 1 (Part 1)", "Episode 2 (Part 2)", "Episode 3", "Episode 4 (Part 1)", "Episode 5 (Part 2)")]
        public void Grouped_NotGrouped_Grouped(string one, string two, string three, string four, string five)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1),
                NamedEpisode(two, 1, 1, 2),
                NamedEpisode(three, 1, 1, 3),
                NamedEpisode(four, 1, 1, 4),
                NamedEpisode(five, 1, 1, 5)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(3);
            ShouldHaveTwoItems(result, mediaItems[0], mediaItems[1]);
            ShouldHaveOneItem(result, mediaItems[2]);
            ShouldHaveTwoItems(result, mediaItems[3], mediaItems[4]);
        }

        [Test]
        [TestCase("Episode 1 (1)", "Episode 2 (2)", "Episode 3 (1)", "Episode 4 (2)")]
        [TestCase("Episode 1 (1) - More", "Episode 2 (2) - Title", "Episode 3 (1) - After", "Episode 4 (2) - Dash")]
        [TestCase("Episode 1 Part 1", "Episode 2 Part 2", "Episode 3 Part 1", "Episode 4 Part 2")]
        [TestCase("Episode 1 (Part 1)", "Episode 2 (Part 2)", "Episode 3 (Part 1)", "Episode 4 (Part 2)")]
        public void Grouped_Grouped(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1),
                NamedEpisode(two, 1, 1, 2),
                NamedEpisode(three, 1, 1, 3),
                NamedEpisode(four, 1, 1, 4)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(2);
            ShouldHaveTwoItems(result, mediaItems[0], mediaItems[1]);
            ShouldHaveTwoItems(result, mediaItems[2], mediaItems[3]);
        }

        [Test]
        [TestCase("Episode 1", "Episode 2 (2)", "Episode 3 (1)", "Episode 4 (2)")]
        [TestCase("Episode 1 - More", "Episode 2 (2) - Title", "Episode 3 (1) - After", "Episode 4 (2) - Dash")]
        [TestCase("Episode 1", "Episode 2 Part 2", "Episode 3 Part 1", "Episode 4 Part 2")]
        [TestCase("Episode 1", "Episode 2 (Part 2)", "Episode 3 (Part 1)", "Episode 4 (Part 2)")]
        public void Part2_Without_Part1(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1),
                NamedEpisode(two, 1, 1, 2),
                NamedEpisode(three, 1, 1, 3),
                NamedEpisode(four, 1, 1, 4)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(3);
            ShouldHaveOneItem(result, mediaItems[0]);
            ShouldHaveOneItem(result, mediaItems[1]);
            ShouldHaveTwoItems(result, mediaItems[2], mediaItems[3]);
        }

        [Test]
        [TestCase("Episode 1", "Episode 2 (2)", "Episode 3 (3)", "Episode 4")]
        [TestCase("Episode 1 - More", "Episode 2 (2) - Title", "Episode 3 (3) - After", "Episode 4 - Dash")]
        [TestCase("Episode 1", "Episode 2 Part 2", "Episode 3 Part 3", "Episode 4")]
        [TestCase("Episode 1", "Episode 2 (Part 2)", "Episode 3 (Part 3)", "Episode 4")]
        public void Part2And3_Without_Part1(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1),
                NamedEpisode(two, 1, 1, 2),
                NamedEpisode(three, 1, 1, 3),
                NamedEpisode(four, 1, 1, 4)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(3);
            ShouldHaveOneItem(result, mediaItems[0]);
            ShouldHaveTwoItems(result, mediaItems[1], mediaItems[2]);
            ShouldHaveOneItem(result, mediaItems[3]);
        }

        [Test]
        [TestCase("Episode 1 (1)", "Episode 3 (3)", "Episode 4", "Episode 5")]
        [TestCase("Episode 1 (1) - More", "Episode 3 (3) - Title", "Episode 4 - After", "Episode 5 - Dash")]
        [TestCase("Episode 1 Part 1", "Episode 3 Part 3", "Episode 4", "Episode 5")]
        [TestCase("Episode 1 (Part 1)", "Episode 3 (Part 3)", "Episode 4", "Episode 5")]
        public void Skip_Part(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1),
                NamedEpisode(two, 1, 1, 3),
                NamedEpisode(three, 1, 1, 4),
                NamedEpisode(four, 1, 1, 5)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(4);
            ShouldHaveOneItem(result, mediaItems[0]);
            ShouldHaveOneItem(result, mediaItems[1]);
            ShouldHaveOneItem(result, mediaItems[2]);
            ShouldHaveOneItem(result, mediaItems[3]);
        }

        [Test]
        [TestCase("Episode 1 (1)", "Episode 3 (1)", "Episode 4 (2)", "Episode 5")]
        [TestCase("Episode 1 (1) - More", "Episode 3 (1) - Title", "Episode 4 (2) - After", "Episode 5 - Dash")]
        [TestCase("Episode 1 Part 1", "Episode 3 Part 1", "Episode 4 Part 2", "Episode 5")]
        [TestCase("Episode 1 (Part 1)", "Episode 3 (Part 1)", "Episode 4 (Part 2)", "Episode 5")]
        public void Repeat_Part(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1),
                NamedEpisode(two, 1, 1, 3),
                NamedEpisode(three, 1, 1, 4),
                NamedEpisode(four, 1, 1, 5)
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(3);
            ShouldHaveOneItem(result, mediaItems[0]);
            ShouldHaveTwoItems(result, mediaItems[1], mediaItems[2]);
            ShouldHaveOneItem(result, mediaItems[3]);
        }

        [Test]
        [TestCase("S1 Episode 1 (1)", "S2 Episode 3 (1)", "S1 Episode 2 (2)", "S1 Episode 5")]
        [TestCase(
            "S1 Episode 1 (1) - More",
            "S2 Episode 3 (1) - Title",
            "S1 Episode 2 (2) - After",
            "S1 Episode 5 - Dash")]
        [TestCase("S1 Episode 1 Part 1", "S2 Episode 3 Part 1", "S1 Episode 2 Part 2", "S1 Episode 5")]
        [TestCase("S1 Episode 1 (Part 1)", "S2 Episode 3 (Part 1)", "S1 Episode 2 (Part 2)", "S1 Episode 5")]
        public void Mixed_Shows_Chronologically(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1, new DateTime(2020, 1, 1)),
                NamedEpisode(two, 2, 1, 3, new DateTime(2020, 1, 2)),
                NamedEpisode(three, 1, 1, 2, new DateTime(2020, 1, 3)),
                NamedEpisode(four, 1, 1, 5, new DateTime(2020, 1, 4))
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, false);

            result.Count.Should().Be(3);
            ShouldHaveTwoItems(result, mediaItems[0], mediaItems[2]);
            ShouldHaveOneItem(result, mediaItems[1]);
            ShouldHaveOneItem(result, mediaItems[3]);
        }

        [Test]
        [TestCase("S1 Episode 1 (1)", "S2 Episode 3 (2)", "S1 Episode 2 (3)", "S1 Episode 5")]
        [TestCase(
            "S1 Episode 1 (1) - More",
            "S2 Episode 3 (2) - Title",
            "S1 Episode 2 (3) - After",
            "S1 Episode 5 - Dash")]
        [TestCase("S1 Episode 1 Part 1", "S2 Episode 3 Part 2", "S1 Episode 2 Part 3", "S1 Episode 5")]
        [TestCase("S1 Episode 1 (Part 1)", "S2 Episode 3 (Part 2)", "S1 Episode 2 (Part 3)", "S1 Episode 5")]
        public void Mixed_Shows_Chronologically_Crossover(string one, string two, string three, string four)
        {
            var mediaItems = new List<MediaItem>
            {
                NamedEpisode(one, 1, 1, 1, new DateTime(2020, 1, 1)),
                NamedEpisode(two, 2, 1, 3, new DateTime(2020, 1, 2)),
                NamedEpisode(three, 1, 1, 2, new DateTime(2020, 1, 3)),
                NamedEpisode(four, 1, 1, 5, new DateTime(2020, 1, 4))
            };

            List<GroupedMediaItem> result = MultiPartEpisodeGrouper.GroupMediaItems(mediaItems, true);

            result.Count.Should().Be(2);
            ShouldHaveMultipleItems(result, mediaItems[0], new List<MediaItem> { mediaItems[1], mediaItems[2] });
            ShouldHaveOneItem(result, mediaItems[3]);
        }

        private static Episode NamedEpisode(
            string title,
            int showId,
            int season,
            int episode,
            DateTime? releaseDate = null) =>
            new()
            {
                EpisodeNumber = episode,
                EpisodeMetadata = new List<EpisodeMetadata>
                {
                    new() { Title = title, ReleaseDate = releaseDate }
                },
                Season = new Season
                {
                    SeasonNumber = season,
                    Show = new Show { Id = showId },
                    ShowId = showId
                }
            };

        private static void ShouldHaveOneItem(IEnumerable<GroupedMediaItem> result, MediaItem item) =>
            result.Filter(g => g.First == item && Optional(g.Additional).Flatten().HeadOrNone() == None)
                .Should().HaveCount(1);

        private static void ShouldHaveTwoItems(
            IEnumerable<GroupedMediaItem> result,
            MediaItem first,
            MediaItem additional) =>
            result.Filter(g => g.First == first && Optional(g.Additional).Flatten().HeadOrNone() == Some(additional))
                .Should().HaveCount(1);

        private static void ShouldHaveMultipleItems(
            IEnumerable<GroupedMediaItem> result,
            MediaItem first,
            List<MediaItem> additional) =>
            result.Filter(
                    g => g.First == first && g.Additional != null && g.Additional.Count == additional.Count &&
                         additional.ForAll(g.Additional.Contains))
                .Should().HaveCount(1);
    }
}
