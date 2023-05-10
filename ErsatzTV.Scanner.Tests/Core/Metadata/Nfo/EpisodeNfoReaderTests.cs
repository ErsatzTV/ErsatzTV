using System.Text;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Scanner.Core.Metadata.Nfo;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Moq;
using NUnit.Framework;
using Serilog;

namespace ErsatzTV.Scanner.Tests.Core.Metadata.Nfo;

[TestFixture]
public class EpisodeNfoReaderTests
{
    [SetUp]
    public void SetUp() => _episodeNfoReader = new EpisodeNfoReader(
        new RecyclableMemoryStreamManager(),
        new Mock<IClient>().Object,
        _logger);

    private readonly ILogger<EpisodeNfoReader> _logger;

    public EpisodeNfoReaderTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

        _logger = loggerFactory.CreateLogger<EpisodeNfoReader>();
    }

    private EpisodeNfoReader _episodeNfoReader;

    [Test]
    public async Task One()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(1);
        }
    }

    [Test]
    public async Task Two()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <showtitle>show</showtitle>
  <title>episode-one</title>
  <episode>1</episode>
  <season>1</season>
</episodedetails>
<episodedetails>
  <showtitle>show</showtitle>
  <title>episode-two</title>
  <episode>2</episode>
  <season>1</season>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(2);
            list.All(nfo => nfo.ShowTitle == "show").Should().BeTrue();
            list.All(nfo => nfo.Season == 1).Should().BeTrue();
            list.Count(nfo => nfo.Title == "episode-one" && nfo.Episode == 1).Should().Be(1);
            list.Count(nfo => nfo.Title == "episode-two" && nfo.Episode == 2).Should().Be(1);
        }
    }

    [Test]
    public async Task UniqueIds()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <uniqueid default=""true"" type=""tvdb"">12345</uniqueid>
  <uniqueid default=""false"" type=""imdb"">tt54321</uniqueid>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(1);
            list[0].UniqueIds.Count.Should().Be(2);
            list[0].UniqueIds.Count(id => id.Default && id.Type == "tvdb" && id.Guid == "12345").Should().Be(1);
            list[0].UniqueIds.Count(id => !id.Default && id.Type == "imdb" && id.Guid == "tt54321").Should().Be(1);
        }
    }

    [Test]
    public async Task No_ContentRating()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <mpaa/>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(1);
            list[0].ContentRating.Should().BeNullOrEmpty();
        }
    }

    [Test]
    public async Task ContentRating()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <mpaa>US:Something</mpaa>
</episodedetails>
<episodedetails>
  <mpaa>US:Something / US:SomethingElse</mpaa>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(2);
            list.Count(nfo => nfo.ContentRating == "US:Something").Should().Be(1);
            list.Count(nfo => nfo.ContentRating == "US:Something / US:SomethingElse").Should().Be(1);
        }
    }

    [Test]
    public async Task No_Plot()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <plot/>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(1);
            list[0].Plot.Should().BeNullOrEmpty();
        }
    }

    [Test]
    public async Task Plot()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <plot>Some Plot</plot>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(1);
            list[0].Plot.Should().Be("Some Plot");
        }
    }

    [Test]
    public async Task Actors()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <actor>
    <name>Name 1</name>
    <role>Role 1</role>
    <thumb>Thumb 1</thumb>
  </actor>
  <actor>
    <name>Name 2</name>
    <role>Role 2</role>
    <thumb>Thumb 2</thumb>
  </actor>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(1);
            list[0].Actors.Count.Should().Be(2);
            list[0].Actors.Count(a => a.Name == "Name 1" && a.Role == "Role 1" && a.Thumb == "Thumb 1")
                .Should().Be(1);
            list[0].Actors.Count(a => a.Name == "Name 2" && a.Role == "Role 2" && a.Thumb == "Thumb 2")
                .Should().Be(1);
        }
    }

    [Test]
    public async Task Writers()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <credits>Writer 1</credits>
</episodedetails>
<episodedetails>
  <credits>Writer 2</credits>
  <credits>Writer 3</credits>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(2);
            list.Count(nfo => nfo.Writers.Count == 1 && nfo.Writers[0] == "Writer 1").Should().Be(1);
            list.Count(nfo => nfo.Writers.Count == 2 && nfo.Writers[0] == "Writer 2" && nfo.Writers[1] == "Writer 3")
                .Should().Be(1);
        }
    }

    [Test]
    public async Task Directors()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <director>Director 1</director>
</episodedetails>
<episodedetails>
  <director>Director 2</director>
  <director>Director 3</director>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(2);
            list.Count(nfo => nfo.Directors.Count == 1 && nfo.Directors[0] == "Director 1").Should().Be(1);
            list.Count(
                    nfo => nfo.Directors.Count == 2 && nfo.Directors[0] == "Director 2" &&
                           nfo.Directors[1] == "Director 3")
                .Should().Be(1);
        }
    }

    [Test]
    public async Task Genres()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <genre>Genre 1</genre>
</episodedetails>
<episodedetails>
  <genre>Genre 2</genre>
  <genre>Genre 3</genre>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(2);
            list.Count(nfo => nfo.Genres is ["Genre 1"]).Should().Be(1);
            list.Count(nfo => nfo.Genres is ["Genre 2", "Genre 3"]).Should().Be(1);
        }
    }

    [Test]
    public async Task Tags()
    {
        var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <tag>Tag 1</tag>
</episodedetails>
<episodedetails>
  <tag>Tag 2</tag>
  <tag>Tag 3</tag>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(2);
            list.Count(nfo => nfo.Tags is ["Tag 1"]).Should().Be(1);
            list.Count(nfo => nfo.Tags is ["Tag 2", "Tag 3"]).Should().Be(1);
        }
    }

    [Test]
    public async Task FullSample_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes"" ?>
<episodedetails>
    <title>Filmed Before a Live Studio Audience</title>
    <showtitle>WandaVision</showtitle>
    <ratings>
        <rating name=""imdb"" max=""10"" default=""true"">
            <value>7.500000</value>
            <votes>18766</votes>
        </rating>
        <rating name=""tmdb"" max=""10"">
            <value>7.500000</value>
            <votes>42</votes>
        </rating>
        <rating name=""trakt"" max=""10"">
            <value>6.952780</value>
            <votes>3621</votes>
        </rating>
    </ratings>
    <userrating>0</userrating>
    <top250>0</top250>
    <season>1</season>
    <episode>1</episode>
    <displayseason>-1</displayseason>
    <displayepisode>-1</displayepisode>
    <outline></outline>
    <plot>Wanda and Vision struggle to conceal their powers during dinner with Vision’s boss and his wife.</plot>
    <tagline></tagline>
    <runtime>26</runtime>
    <thumb spoof="""" cache="""" aspect=""thumb"" preview=""https://image.tmdb.org/t/p/w780/cbe8l0Hnbvu07ePgoOopyWYrcdL.jpg"">https://image.tmdb.org/t/p/original/cbe8l0Hnbvu07ePgoOopyWYrcdL.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""thumb"" preview=""https://image.tmdb.org/t/p/w780/oNCzeCXFanVEWNpzRzyffhLLfZs.jpg"">https://image.tmdb.org/t/p/original/oNCzeCXFanVEWNpzRzyffhLLfZs.jpg</thumb>
    <mpaa>Australia:TV-14</mpaa>
    <playcount>1</playcount>
    <lastplayed>2021-03-27</lastplayed>
    <id>1830976</id>
    <uniqueid type=""imdb"">tt9601584</uniqueid>
    <uniqueid type=""tmdb"" default=""true"">1830976</uniqueid>
    <uniqueid type=""tvdb"">8042515</uniqueid>
    <genre>Sci-Fi &amp; Fantasy</genre>
    <genre>Mystery</genre>
    <genre>Drama</genre>
    <credits>Jac Schaeffer</credits>
    <director>Matt Shakman</director>
    <premiered>2021-01-15</premiered>
    <year>2021</year>
    <status></status>
    <code></code>
    <aired>2021-01-15</aired>
    <studio>Disney+ (US)</studio>
    <trailer></trailer>
    <fileinfo>
        <streamdetails>
            <video>
                <codec>h264</codec>
                <aspect>1.777778</aspect>
                <width>1280</width>
                <height>720</height>
                <durationinseconds>1593</durationinseconds>
                <stereomode></stereomode>
            </video>
            <audio>
                <codec>aac</codec>
                <language>eng</language>
                <channels>2</channels>
            </audio>
        </streamdetails>
    </fileinfo>
    <actor>
        <name>Randall Park</name>
        <role>Jimmy Woo</role>
        <order>4</order>
        <thumb>https://image.tmdb.org/t/p/original/1QJ4cBQZoOaLR8Hc3V2NgBLvB0f.jpg</thumb>
    </actor>
    <actor>
        <name>Kat Dennings</name>
        <role>Darcy Lewis / The Escape Artist</role>
        <order>5</order>
        <thumb>https://image.tmdb.org/t/p/original/rrfyo9z1wW5nY9ZsFlj1Ozfj9g2.jpg</thumb>
    </actor>
    <resume>
        <position>0.000000</position>
        <total>0.000000</total>
    </resume>
    <dateadded>2021-02-02 11:57:44</dateadded>
</episodedetails>"));

        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();

        foreach (EpisodeNfo nfo in result.RightToSeq().Flatten())
        {
            nfo.ShowTitle.Should().Be("WandaVision");
            nfo.Title.Should().Be("Filmed Before a Live Studio Audience");
            nfo.Episode.Should().Be(1);
            nfo.Season.Should().Be(1);
            nfo.ContentRating.Should().Be("Australia:TV-14");

            nfo.Aired.IsSome.Should().BeTrue();
            foreach (DateTime aired in nfo.Aired)
            {
                aired.Should().Be(new DateTime(2021, 01, 15));
            }

            nfo.Plot.Should().Be(
                "Wanda and Vision struggle to conceal their powers during dinner with Vision’s boss and his wife.");
            nfo.Actors.Should().BeEquivalentTo(
                new List<ActorNfo>
                {
                    new()
                    {
                        Name = "Randall Park", Order = 4, Role = "Jimmy Woo",
                        Thumb = "https://image.tmdb.org/t/p/original/1QJ4cBQZoOaLR8Hc3V2NgBLvB0f.jpg"
                    },
                    new()
                    {
                        Name = "Kat Dennings", Order = 5, Role = "Darcy Lewis / The Escape Artist",
                        Thumb = "https://image.tmdb.org/t/p/original/rrfyo9z1wW5nY9ZsFlj1Ozfj9g2.jpg"
                    }
                });
            nfo.Writers.Should().BeEquivalentTo(new List<string> { "Jac Schaeffer" });
            nfo.Directors.Should().BeEquivalentTo(new List<string> { "Matt Shakman" });
            nfo.UniqueIds.Should().BeEquivalentTo(
                new List<UniqueIdNfo>
                {
                    new() { Type = "imdb", Guid = "tt9601584", Default = false },
                    new() { Type = "tmdb", Guid = "1830976", Default = true },
                    new() { Type = "tvdb", Guid = "8042515", Default = false }
                });
        }
    }

    [Test]
    public async Task Invalid_Characters_Should_Abort_And_Return_Nfo()
    {
        string sourceFile = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Resources",
            "Nfo",
            "EpisodeInvalidCharacters.nfo");
        Either<BaseError, List<EpisodeNfo>> result = await _episodeNfoReader.ReadFromFile(sourceFile);

        result.IsRight.Should().BeTrue();
        foreach (List<EpisodeNfo> list in result.RightToSeq())
        {
            list.Count.Should().Be(1);
            list[0].Title.Should().Be("Test Title");
        }
    }
}
