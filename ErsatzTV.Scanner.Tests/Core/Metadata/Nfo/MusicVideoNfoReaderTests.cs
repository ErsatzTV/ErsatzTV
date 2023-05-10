﻿using System.Text;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Scanner.Core.Metadata.Nfo;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IO;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Scanner.Tests.Core.Metadata.Nfo;

[TestFixture]
public class MusicVideoNfoReaderTests
{
    [SetUp]
    public void SetUp() => _musicVideoNfoReader = new MusicVideoNfoReader(
        new RecyclableMemoryStreamManager(),
        new Mock<IClient>().Object,
        new NullLogger<MusicVideoNfoReader>());

    private MusicVideoNfoReader _musicVideoNfoReader;

    [Test]
    public async Task ParsingNfo_Should_Return_Error()
    {
        await using var stream =
            new MemoryStream(Encoding.UTF8.GetBytes(@"https://www.themoviedb.org/movie/11-star-wars"));

        Either<BaseError, MusicVideoNfo> result = await _musicVideoNfoReader.Read(stream);

        result.IsLeft.Should().BeTrue();
    }

    [Test]
    public async Task MetadataNfo_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(@"<musicvideo></musicvideo>"));

        Either<BaseError, MusicVideoNfo> result = await _musicVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
    }

    [Test]
    public async Task CombinationNfo_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<musicvideo></musicvideo>
https://www.themoviedb.org/movie/11-star-wars"));

        Either<BaseError, MusicVideoNfo> result = await _musicVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
    }

    [Test]
    public async Task FullSample_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                NormalizeLineEndingsLF(
                    @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes"" ?>
<musicvideo>
    <title>Dancing Queen</title>
    <userrating>0</userrating>
    <top250>0</top250>
    <track>-1</track>
    <album>Arrival</album>
    <outline></outline>
    <plot>Dancing Queen est un des tubes emblématiques de l&apos;ère disco produits par le groupe suédois ABBA en 1976. Ce tube connaît un regain de popularité en 1994 lors de la sortie de Priscilla, folle du désert, et fait « presque » partie de la distribution du film Muriel.&#x0A;Le groupe a également enregistré une version espagnole de ce titre, La reina del baile, pour le marché d&apos;Amérique latine. On peut retrouver ces versions en espagnol des succès de ABBA sur l&apos;abum Oro. Le 18 juin 1976, ABBA a interprété cette chanson lors d&apos;un spectacle télévisé organisé en l&apos;honneur du roi Charles XVI Gustave de Suède, qui venait de se marier. Le titre sera repris en 2011 par Glee dans la saison 2, épisode 20.</plot>
    <tagline></tagline>
    <runtime>2</runtime>
    <thumb preview=""https://www.theaudiodb.com/images/media/album/thumb/arrival-4ee244732bbde.jpg/preview"">https://www.theaudiodb.com/images/media/album/thumb/arrival-4ee244732bbde.jpg</thumb>
    <thumb preview=""https://assets.fanart.tv/preview/music/d87e52c5-bb8d-4da8-b941-9f4928627dc8/albumcover/arrival-548ab7a698b49.jpg"">https://assets.fanart.tv/fanart/music/d87e52c5-bb8d-4da8-b941-9f4928627dc8/albumcover/arrival-548ab7a698b49.jpg</thumb>
    <mpaa></mpaa>
    <playcount>0</playcount>
    <lastplayed></lastplayed>
    <id></id>
    <genre>Pop</genre>
    <year>1976</year>
    <status></status>
	<director>Director 1</director>
	<director>Director 2</director>
	<director>Director 3</director>
	<director>Director 4</director>
    <code></code>
    <aired></aired>
    <trailer></trailer>
    <fileinfo>
        <streamdetails>
            <video>
                <codec>hevc</codec>
                <aspect>1.792230</aspect>
                <width>716</width>
                <height>568</height>
                <durationinseconds>143</durationinseconds>
                <stereomode></stereomode>
            </video>
            <audio>
                <codec>ac3</codec>
                <language>eng</language>
                <channels>2</channels>
            </audio>
        </streamdetails>
    </fileinfo>
    <artist>ABBA</artist>
    <resume>
        <position>0.000000</position>
        <total>0.000000</total>
    </resume>
    <dateadded>2018-09-10 09:46:06</dateadded>
</musicvideo>")));

        Either<BaseError, MusicVideoNfo> result = await _musicVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();

        foreach (MusicVideoNfo nfo in result.RightToSeq())
        {
            nfo.Artists.Should().BeEquivalentTo(new List<string> { "ABBA" });
            nfo.Title.Should().Be("Dancing Queen");
            nfo.Album.Should().Be("Arrival");
            nfo.Plot.Should().Be(
                NormalizeLineEndingsLF(
                    @"Dancing Queen est un des tubes emblématiques de l'ère disco produits par le groupe suédois ABBA en 1976. Ce tube connaît un regain de popularité en 1994 lors de la sortie de Priscilla, folle du désert, et fait « presque » partie de la distribution du film Muriel.
Le groupe a également enregistré une version espagnole de ce titre, La reina del baile, pour le marché d'Amérique latine. On peut retrouver ces versions en espagnol des succès de ABBA sur l'abum Oro. Le 18 juin 1976, ABBA a interprété cette chanson lors d'un spectacle télévisé organisé en l'honneur du roi Charles XVI Gustave de Suède, qui venait de se marier. Le titre sera repris en 2011 par Glee dans la saison 2, épisode 20."));

            nfo.Year.Should().Be(1976);
            nfo.Aired.IsNone.Should().BeTrue();
            nfo.Genres.Should().BeEquivalentTo(new List<string> { "Pop" });
            nfo.Track.Should().Be(-1);
        }
    }

    [Test]
    public async Task MetadataNfo_With_Tags_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(@"<musicvideo><tag>Test Tag</tag></musicvideo>"));

        Either<BaseError, MusicVideoNfo> result = await _musicVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (MusicVideoNfo nfo in result.RightToSeq())
        {
            nfo.Tags.Should().BeEquivalentTo(new List<string> { "Test Tag" });
        }
    }

    [Test]
    public async Task MetadataNfo_With_Aired_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(@"<musicvideo><aired>2022-02-03</aired></musicvideo>"));

        Either<BaseError, MusicVideoNfo> result = await _musicVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (MusicVideoNfo nfo in result.RightToSeq())
        {
            nfo.Aired.IsSome.Should().BeTrue();
            foreach (DateTime aired in nfo.Aired)
            {
                aired.Should().Be(new DateTime(2022, 02, 03));
            }
        }
    }

    [Test]
    public async Task MetadataNfo_With_Studios_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(@"<musicvideo><studio>Test Studio</studio></musicvideo>"));

        Either<BaseError, MusicVideoNfo> result = await _musicVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (MusicVideoNfo nfo in result.RightToSeq())
        {
            nfo.Studios.Should().BeEquivalentTo(new List<string> { "Test Studio" });
        }
    }

    [Test]
    public async Task MetadataNfo_With_Directors_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(@"<musicvideo><director>Test Director</director></musicvideo>"));

        Either<BaseError, MusicVideoNfo> result = await _musicVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (MusicVideoNfo nfo in result.RightToSeq())
        {
            nfo.Directors.Should().BeEquivalentTo(new List<string> { "Test Director" });
        }
    }

    private static string NormalizeLineEndingsLF(string str) =>
        str
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");
}
