using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Tests.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata;

[TestFixture]
public class LocalSubtitlesProviderTests
{
    [Test]
    public void Test()
    {
        var cultures = new List<CultureInfo>
        {
            CultureInfo.GetCultureInfo("en-US")
        };

        var fakeFiles = new List<FakeFileEntry>
        {
            new("/some/movie/path.en.srt"),
            new("/some/movie/path.eng.ass")
        };

        var provider = new LocalSubtitlesProvider(new FakeLocalFileSystem(fakeFiles));
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
                            new() { Path = @"/some/movie/path.mkv" }
                        }
                    }
                }
            });

        result.Count.Should().BeGreaterThan(0);
    }
}
