using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata;

[TestFixture]
public class LocalSubtitlesProviderTests
{
    // test cases are from plex's example folder layout here
    // https://support.plex.tv/articles/200471133-adding-local-subtitles-to-your-media/
    // /Movies
    //    /Avatar (2009)
    //       Avatar (2009).mkv
    //       Avatar (2009).eng.srt
    //       Avatar (2009).en.forced.ass
    //       Avatar (2009).en.sdh.srt
    //       Avatar (2009).de.srt
    //       Avatar (2009).de.sdh.forced.srt
    
    [Test]
    public void Should_Find_All_Languages_Codecs_And_Flags()
    {
        // normally this will have a full list from the database, but we just need these two for testing
        var cultures = new List<CultureInfo>
        {
            CultureInfo.GetCultureInfo("en-US"),
            CultureInfo.GetCultureInfo("de-DE")
        };

        var fakeFiles = new List<FakeFileEntry>
        {
            new(@"/Movies/Avatar (2009)/Avatar (2009).mkv"),
            new(@"/Movies/Avatar (2009)/Avatar (2009).eng.srt"),
            new(@"/Movies/Avatar (2009)/Avatar (2009).en.forced.ass"),
            new(@"/Movies/Avatar (2009)/Avatar (2009).en.sdh.srt"),
            new(@"/Movies/Avatar (2009)/Avatar (2009).de.srt"),
            new(@"/Movies/Avatar (2009)/Avatar (2009).de.sdh.forced.srt")
        };

        var provider = new LocalSubtitlesProvider(
            new Mock<IMediaItemRepository>().Object,
            new Mock<IMetadataRepository>().Object,
            new FakeLocalFileSystem(fakeFiles),
            new Mock<ILogger<LocalSubtitlesProvider>>().Object);

        List<Subtitle> result = provider.LocateExternalSubtitles(
            cultures,
            new Movie
            {
                MediaVersions = new List<MediaVersion>
                {
                    new()
                    {
                        MediaFiles = new List<MediaFile>
                        {
                            new() { Path = @"/Movies/Avatar (2009)/Avatar (2009).mkv" }
                        }
                    }
                }
            });

        result.Count.Should().Be(5);
        result.Count(s => s.Language == "eng").Should().Be(3);
        result.Count(s => s.Language == "deu").Should().Be(2);
        result.Count(s => s.Forced).Should().Be(2);
        result.Count(s => s.SDH).Should().Be(2);
        result.Count(s => s.Codec == "subrip").Should().Be(4);
        result.Count(s => s.Codec == "ass").Should().Be(1);
    }
}
