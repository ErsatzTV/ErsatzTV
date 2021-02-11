using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata
{
    [TestFixture]
    public class FallbackMetadataProviderTests
    {
        [Test]
        [TestCase("Awesome Show - s01e02.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show - S01E02.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show - s1e2.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show - S1E2.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show - s01e02 - Episode Title.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show - S01E02 - Episode Title.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show - s1e2 - Episode Title.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show - S1E2 - Episode Title.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show (2021) - s01e02 - Episode Title.mkv", "Awesome Show (2021)", 1, 2)]
        [TestCase("Awesome Show (2021) - S01E02 - Episode Title.mkv", "Awesome Show (2021)", 1, 2)]
        [TestCase("Awesome Show (2021) - s1e2 - Episode Title.mkv", "Awesome Show (2021)", 1, 2)]
        [TestCase("Awesome Show (2021) - S1E2 - Episode Title.mkv", "Awesome Show (2021)", 1, 2)]
        [TestCase("Awesome Show - s01e02 - Episode Title-720p.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show - S01E02 - Episode Title-720p.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show - s1e2 - Episode Title-720p.mkv", "Awesome Show", 1, 2)]
        [TestCase("Awesome Show - S1E2 - Episode Title-720p.mkv", "Awesome Show", 1, 2)]
        [TestCase(
            "Awesome Show (2021) - S01E02 - Description; More Description (1080p QUALITY codec GROUP).mkv",
            "Awesome Show (2021)",
            1,
            2)]
        [TestCase(
            "Awesome.Show.S01E02.Description.more.Description.QUAlity.codec.CODEC-GROUP.mkv",
            "Awesome.Show",
            1,
            2)]
        public void GetFallbackMetadata_ShouldHandleVariousFormats(string path, string title, int season, int episode)
        {
            MediaMetadata metadata = FallbackMetadataProvider.GetFallbackMetadata(new MediaItem { Path = path });

            metadata.MediaType.Should().Be(MediaType.TvShow);
            metadata.Title.Should().Be(title);
            metadata.SeasonNumber.Should().Be(season);
            metadata.EpisodeNumber.Should().Be(episode);
        }
    }
}
