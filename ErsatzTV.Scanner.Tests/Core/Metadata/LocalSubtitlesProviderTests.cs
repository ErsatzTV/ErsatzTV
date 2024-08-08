﻿using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Scanner.Core.Metadata;
using ErsatzTV.Scanner.Tests.Core.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ErsatzTV.Scanner.Tests.Core.Metadata;

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
    public void Should_Find_All_Languages_Codecs_And_Flags_With_Full_Paths()
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

            // non-uniform (lower-case) extensions should also work
            new(@"/Movies/Avatar (2009)/Avatar (2009).DE.SDH.FORCED.SRT")
        };

        var provider = new LocalSubtitlesProvider(
            Substitute.For<IMediaItemRepository>(),
            Substitute.For<IMetadataRepository>(),
            new FakeLocalFileSystem(fakeFiles),
            Substitute.For<ILogger<LocalSubtitlesProvider>>());

        List<Subtitle> result = provider.LocateExternalSubtitles(
            cultures,
            @"/Movies/Avatar (2009)/Avatar (2009).mkv",
            true);

        result.Count.Should().Be(5);
        result.Count(s => s.Language == "eng").Should().Be(3);
        result.Count(s => s.Language == "deu").Should().Be(2);
        result.Count(s => s.Forced).Should().Be(2);
        result.Count(s => s.SDH).Should().Be(2);
        result.Count(s => s.Codec == "subrip").Should().Be(4);
        result.Count(s => s.Codec == "ass").Should().Be(1);
        result.All(s => s.Path.Contains(@"/Movies/Avatar (2009)/")).Should().BeTrue();
    }

    [Test]
    public void Should_Find_All_Languages_Codecs_And_Flags_With_File_Names()
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
            new(@"/Movies/Avatar (2009)/Avatar (2009).forced.en.ass"),
            new(@"/Movies/Avatar (2009)/Avatar (2009).en.sdh.srt"),
            new(@"/Movies/Avatar (2009)/Avatar (2009).sdh.en.srt"),
            new(@"/Movies/Avatar (2009)/Avatar (2009).de.srt"),

            // non-uniform (lower-case) extensions should also work
            new(@"/Movies/Avatar (2009)/Avatar (2009).DE.SDH.FORCED.SRT")
        };

        var provider = new LocalSubtitlesProvider(
            Substitute.For<IMediaItemRepository>(),
            Substitute.For<IMetadataRepository>(),
            new FakeLocalFileSystem(fakeFiles),
            Substitute.For<ILogger<LocalSubtitlesProvider>>());

        List<Subtitle> result = provider.LocateExternalSubtitles(
            cultures,
            @"/Movies/Avatar (2009)/Avatar (2009).mkv",
            false);

        result.Count.Should().Be(7);
        result.Count(s => s.Language == "eng").Should().Be(5);
        result.Count(s => s.Language == "deu").Should().Be(2);
        result.Count(s => s.Forced).Should().Be(3);
        result.Count(s => s.SDH).Should().Be(3);
        result.Count(s => s.Codec == "subrip").Should().Be(5);
        result.Count(s => s.Codec == "ass").Should().Be(2);
        result.Count(s => s.Path.Contains(@"/Movies/Avatar (2009)/")).Should().Be(0);
    }
}
