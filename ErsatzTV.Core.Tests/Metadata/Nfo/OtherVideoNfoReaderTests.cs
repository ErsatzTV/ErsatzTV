using System.Text;
using Bugsnag;
using ErsatzTV.Core.Metadata.Nfo;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata.Nfo;

[TestFixture]
public class OtherVideoNfoReaderTests
{
    [SetUp]
    public void SetUp() => _otherVideoNfoReader = new OtherVideoNfoReader(new Mock<IClient>().Object);

    private OtherVideoNfoReader _otherVideoNfoReader;

    [Test]
    public async Task ParsingNfo_Should_Return_Error()
    {
        await using var stream =
            new MemoryStream(Encoding.UTF8.GetBytes(@"https://www.themoviedb.org/movie/11-star-wars"));

        Either<BaseError, OtherVideoNfo> result = await _otherVideoNfoReader.Read(stream);

        result.IsLeft.Should().BeTrue();
    }

    [Test]
    public async Task MetadataNfo_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(@"<movie></movie>"));

        Either<BaseError, OtherVideoNfo> result = await _otherVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
    }

    [Test]
    public async Task CombinationNfo_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<movie></movie>
https://www.themoviedb.org/movie/11-star-wars"));

        Either<BaseError, OtherVideoNfo> result = await _otherVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
    }

    [Test]
    public async Task FullSample_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes"" ?>
<movie>
    <title>Zack Snyder&apos;s Justice League</title>
    <originaltitle>Zack Snyder&apos;s Justice League</originaltitle>
    <sorttitle>Justice League 2</sorttitle>
    <ratings>
        <rating name=""imdb"" max=""10"" default=""true"">
            <value>8.300000</value>
            <votes>197786</votes>
        </rating>
        <rating name=""themoviedb"" max=""10"">
            <value>8.700000</value>
            <votes>3461</votes>
        </rating>
        <rating name=""trakt"" max=""10"">
            <value>8.195670</value>
            <votes>4247</votes>
        </rating>
    </ratings>
    <userrating>0</userrating>
    <top250>140</top250>
    <outline></outline>
    <plot>Determined to ensure Superman&apos;s ultimate sacrifice was not in vain, Bruce Wayne aligns forces with Diana Prince with plans to recruit a team of metahumans to protect the world from an approaching threat of catastrophic proportions.</plot>
    <tagline></tagline>
    <runtime>242</runtime>
    <thumb spoof="""" cache="""" aspect=""poster"" preview="""">https://assets.fanart.tv/fanart/movies/791373/movieposter/zack-snyders-justice-league-603fdb873f474.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""poster"" preview="""">https://image.tmdb.org/t/p/original/tnAuB8q5vv7Ax9UAEje5Xi4BXik.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""landscape"" preview="""">https://assets.fanart.tv/fanart/movies/791373/moviethumb/zack-snyders-justice-league-6050310135cf6.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""landscape"" preview="""">https://image.tmdb.org/t/p/original/wcYBuOZDP6Vi8Ye4qax3Zx9dCan.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""keyart"" preview="""">https://assets.fanart.tv/fanart/movies/791373/movieposter/zack-snyders-justice-league-603fdba9bdd16.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""clearlogo"" preview="""">https://assets.fanart.tv/fanart/movies/791373/hdmovielogo/zack-snyders-justice-league-5ed3f2e4952e9.png</thumb>
    <thumb spoof="""" cache="""" aspect=""banner"" preview="""">https://assets.fanart.tv/fanart/movies/791373/moviebanner/zack-snyders-justice-league-6050049514d4c.jpg</thumb>
    <fanart>
        <thumb colors="""" preview=""https://assets.fanart.tv/preview/movies/791373/moviebackground/zack-snyders-justice-league-5fee5b9fe0e0d.jpg"">https://assets.fanart.tv/fanart/movies/791373/moviebackground/zack-snyders-justice-league-5fee5b9fe0e0d.jpg</thumb>
        <thumb colors="""" preview=""https://image.tmdb.org/t/p/w780/43NwryODVEsbBDC0jK3wYfVyb5q.jpg"">https://image.tmdb.org/t/p/original/43NwryODVEsbBDC0jK3wYfVyb5q.jpg</thumb>
    </fanart>
    <mpaa>Australia:M</mpaa>
    <playcount>0</playcount>
    <lastplayed></lastplayed>
    <id>791373</id>
    <uniqueid type=""imdb"">tt12361974</uniqueid>
    <uniqueid type=""tmdb"" default=""true"">791373</uniqueid>
    <genre>SuperHero</genre>
    <tag>TV Recording</tag>
    <set>
        <name>Justice League Collection</name>
        <overview>Based on the DC Comics superhero team</overview>
    </set>
    <country>USA</country>
    <credits>Chris Terrio</credits>
    <director>Zack Snyder</director>
    <premiered>2021-03-18</premiered>
    <year>2021</year>
    <status></status>
    <code></code>
    <aired></aired>
    <studio>Warner Bros. Pictures</studio>
    <trailer></trailer>
    <fileinfo>
        <streamdetails>
            <video>
                <codec>hevc</codec>
                <aspect>1.777778</aspect>
                <width>1920</width>
                <height>1080</height>
                <durationinseconds>14528</durationinseconds>
                <stereomode></stereomode>
            </video>
            <audio>
                <codec>ac3</codec>
                <language>eng</language>
                <channels>6</channels>
            </audio>
            <audio>
                <codec>ac3</codec>
                <language>fre</language>
                <channels>6</channels>
            </audio>
            <subtitle>
                <language>eng</language>
            </subtitle>
        </streamdetails>
    </fileinfo>
    <actor>
        <name>Ben Affleck</name>
        <role>Bruce Wayne / Batman</role>
        <order>0</order>
        <thumb>https://image.tmdb.org/t/p/original/u525jeDOzg9hVdvYfeehTGnw7Aa.jpg</thumb>
    </actor>
    <actor>
        <name>Henry Cavill</name>
        <role>Clark Kent / Superman / Kal-El</role>
        <order>1</order>
        <thumb>https://image.tmdb.org/t/p/original/hErUwonrQgY5Y7RfxOfv8Fq11MB.jpg</thumb>
    </actor>
    <actor>
        <name>Gal Gadot</name>
        <role>Diana Prince / Wonder Woman</role>
        <order>2</order>
        <thumb>https://image.tmdb.org/t/p/original/fysvehTvU6bE3JgxaOTRfvQJzJ4.jpg</thumb>
    </actor>
    <resume>
        <position>0.000000</position>
        <total>0.000000</total>
    </resume>
    <dateadded>2021-03-26 11:35:50</dateadded>
</movie>"));

        Either<BaseError, OtherVideoNfo> result = await _otherVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();

        foreach (OtherVideoNfo nfo in result.RightToSeq())
        {
            nfo.Title.Should().Be("Zack Snyder's Justice League");
            nfo.SortTitle.Should().Be("Justice League 2");
            nfo.Outline.Should().BeNullOrEmpty();
            nfo.Year.Should().Be(2021);
            nfo.ContentRating.Should().Be("Australia:M");

            nfo.Premiered.IsSome.Should().BeTrue();
            foreach (DateTime premiered in nfo.Premiered)
            {
                premiered.Should().Be(new DateTime(2021, 03, 18));
            }

            nfo.Plot.Should().Be(
                "Determined to ensure Superman's ultimate sacrifice was not in vain, Bruce Wayne aligns forces with Diana Prince with plans to recruit a team of metahumans to protect the world from an approaching threat of catastrophic proportions.");
            nfo.Tagline.Should().BeNullOrEmpty();
            nfo.Genres.Should().BeEquivalentTo(new List<string> { "SuperHero" });
            nfo.Tags.Should().BeEquivalentTo(new List<string> { "TV Recording" });
            nfo.Studios.Should().BeEquivalentTo(new List<string> { "Warner Bros. Pictures" });
            nfo.Actors.Should().BeEquivalentTo(
                new List<ActorNfo>
                {
                    new()
                    {
                        Name = "Ben Affleck", Order = 0, Role = "Bruce Wayne / Batman",
                        Thumb = "https://image.tmdb.org/t/p/original/u525jeDOzg9hVdvYfeehTGnw7Aa.jpg"
                    },
                    new()
                    {
                        Name = "Henry Cavill", Order = 1, Role = "Clark Kent / Superman / Kal-El",
                        Thumb = "https://image.tmdb.org/t/p/original/hErUwonrQgY5Y7RfxOfv8Fq11MB.jpg"
                    },
                    new()
                    {
                        Name = "Gal Gadot", Order = 2, Role = "Diana Prince / Wonder Woman",
                        Thumb = "https://image.tmdb.org/t/p/original/fysvehTvU6bE3JgxaOTRfvQJzJ4.jpg"
                    }
                });
            nfo.Writers.Should().BeEquivalentTo(new List<string> { "Chris Terrio" });
            nfo.Directors.Should().BeEquivalentTo(new List<string> { "Zack Snyder" });
            nfo.UniqueIds.Should().BeEquivalentTo(
                new List<UniqueIdNfo>
                {
                    new() { Type = "imdb", Guid = "tt12361974", Default = false },
                    new() { Type = "tmdb", Guid = "791373", Default = true }
                });
        }
    }

    [Test]
    public async Task MetadataNfo_With_Tag_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(@"<movie><tag>Test Tag</tag></movie>"));

        Either<BaseError, OtherVideoNfo> result = await _otherVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (OtherVideoNfo nfo in result.RightToSeq())
        {
            nfo.Tags.Should().BeEquivalentTo(new List<string> { "Test Tag" });
        }
    }

    [Test]
    public async Task MetadataNfo_With_Outline_Should_Return_Nfo()
    {
        await using var stream =
            new MemoryStream(Encoding.UTF8.GetBytes(@"<movie><outline>Test Outline</outline></movie>"));

        Either<BaseError, OtherVideoNfo> result = await _otherVideoNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (OtherVideoNfo nfo in result.RightToSeq())
        {
            nfo.Outline.Should().Be("Test Outline");
        }
    }
}
