using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata;

[TestFixture]
public class FallbackMetadataProviderTests
{
    [SetUp]
    public void SetUp() => _fallbackMetadataProvider = new FallbackMetadataProvider();

    private FallbackMetadataProvider _fallbackMetadataProvider;

    [Test]
    [TestCase("Awesome Show - s01e02.mkv", 1, 2)]
    [TestCase("Awesome Show - S01E02.mkv", 1, 2)]
    [TestCase("Awesome Show - s1e2.mkv", 1, 2)]
    [TestCase("Awesome Show - S1E2.mkv", 1, 2)]
    [TestCase("Awesome Show - s01e02 - Episode Title.mkv", 1, 2)]
    [TestCase("Awesome Show - S01E02 - Episode Title.mkv", 1, 2)]
    [TestCase("Awesome Show - s1e2 - Episode Title.mkv", 1, 2)]
    [TestCase("Awesome Show - S1E2 - Episode Title.mkv", 1, 2)]
    [TestCase("Awesome Show (2021) - s01e02 - Episode Title.mkv", 1, 2)]
    [TestCase("Awesome Show (2021) - S01E02 - Episode Title.mkv", 1, 2)]
    [TestCase("Awesome Show (2021) - s1e2 - Episode Title.mkv", 1, 2)]
    [TestCase("Awesome Show (2021) - S1E2 - Episode Title.mkv", 1, 2)]
    [TestCase("Awesome Show - s01e02 - Episode Title-720p.mkv", 1, 2)]
    [TestCase("Awesome Show - S01E02 - Episode Title-720p.mkv", 1, 2)]
    [TestCase("Awesome Show - s1e2 - Episode Title-720p.mkv", 1, 2)]
    [TestCase("Awesome Show - S1E2 - Episode Title-720p.mkv", 1, 2)]
    [TestCase(
        "Awesome Show (2021) - S01E02 - Description; More Description (1080p QUALITY codec GROUP).mkv",
        1,
        2)]
    [TestCase(
        "Awesome.Show.S01E02.Description.more.Description.QUAlity.codec.CODEC-GROUP.mkv",
        1,
        2)]
    public void GetFallbackMetadata_ShouldHandleVariousFormats(string path, int season, int episode)
    {
        List<EpisodeMetadata> metadata = _fallbackMetadataProvider.GetFallbackMetadata(
            new Episode
            {
                LibraryPath = new LibraryPath(),
                MediaVersions = new List<MediaVersion>
                {
                    new()
                    {
                        MediaFiles = new List<MediaFile>
                        {
                            new() { Path = path }
                        }
                    }
                }
            });

        metadata.Count.Should().Be(1);
        // TODO: how can we test season number? do we need to?
        // metadata.Season.Should().Be(season);
        metadata.Head().EpisodeNumber.Should().Be(episode);
    }

    [Test]
    [TestCase("Awesome Show - s01e02-s01e03.mkv", 1, 2, 3)]
    [TestCase("Awesome Show - s01e02-whatever-s01e03-whatever2.mkv", 1, 2, 3)]
    [TestCase("Awesome Show - s01e02e03.mkv", 1, 2, 3)]
    [TestCase("Awesome Show - s01e02-03.mkv", 1, 2, 3)]
    public void GetFallbackMetadata_Should_Handle_Two_Episode_Formats(
        string path,
        int season,
        int episode1,
        int episode2)
    {
        List<EpisodeMetadata> metadata = _fallbackMetadataProvider.GetFallbackMetadata(
            new Episode
            {
                LibraryPath = new LibraryPath(),
                MediaVersions = new List<MediaVersion>
                {
                    new()
                    {
                        MediaFiles = new List<MediaFile>
                        {
                            new() { Path = path }
                        }
                    }
                }
            });

        metadata.Count.Should().Be(2);
        metadata.Map(m => m.EpisodeNumber).Should().BeEquivalentTo(new[] { episode1, episode2 });
    }

    [Test]
    [TestCase("Something (2021).mkv", "Something")]
    [TestCase("Something Else (2021).mkv", "Something Else")]
    public void GetFallbackMetadata_Should_Set_Proper_Movie_Title(string path, string expectedTitle)
    {
        MovieMetadata metadata = _fallbackMetadataProvider.GetFallbackMetadata(
            new Movie
            {
                LibraryPath = new LibraryPath(),
                MediaVersions = new List<MediaVersion>
                {
                    new()
                    {
                        MediaFiles = new List<MediaFile>
                        {
                            new() { Path = path }
                        }
                    }
                }
            });

        metadata.Should().NotBeNull();
        metadata.Title.Should().Be(expectedTitle);
    }
}