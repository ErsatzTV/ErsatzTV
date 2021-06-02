using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErsatzTV.Core.Metadata.Nfo;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata.Nfo
{
    [TestFixture]
    public class EpisodeNfoReaderTests
    {
        [Test]
        public async Task One()
        {
            var reader = new EpisodeNfoReader();
            var stream = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
</episodedetails>"));

            List<TvShowEpisodeNfo> result = await reader.Read(stream);

            result.Count.Should().Be(1);
        }

        [Test]
        public async Task Two()
        {
            var reader = new EpisodeNfoReader();
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

            List<TvShowEpisodeNfo> result = await reader.Read(stream);

            result.Count.Should().Be(2);
            result.All(nfo => nfo.ShowTitle == "show").Should().BeTrue();
            result.All(nfo => nfo.Season == 1).Should().BeTrue();
            result.Count(nfo => nfo.Title == "episode-one" && nfo.Episode == 1).Should().Be(1);
            result.Count(nfo => nfo.Title == "episode-two" && nfo.Episode == 2).Should().Be(1);
        }

        [Test]
        public async Task UniqueIds()
        {
            var reader = new EpisodeNfoReader();
            var stream = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <uniqueid default=""true"" type=""tvdb"">12345</uniqueid>
  <uniqueid default=""false"" type=""imdb"">tt54321</uniqueid>
</episodedetails>"));

            List<TvShowEpisodeNfo> result = await reader.Read(stream);

            result.Count.Should().Be(1);
            result[0].UniqueIds.Count.Should().Be(2);
            result[0].UniqueIds.Count(id => id.Default && id.Type == "tvdb" && id.Guid == "12345").Should().Be(1);
            result[0].UniqueIds.Count(id => !id.Default && id.Type == "imdb" && id.Guid == "tt54321").Should().Be(1);
        }

        [Test]
        public async Task No_ContentRating()
        {
            var reader = new EpisodeNfoReader();
            var stream = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <mpaa/>
</episodedetails>"));

            List<TvShowEpisodeNfo> result = await reader.Read(stream);

            result.Count.Should().Be(1);
            result[0].ContentRating.Should().BeNullOrEmpty();
        }

        [Test]
        public async Task ContentRating()
        {
            var reader = new EpisodeNfoReader();
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

            List<TvShowEpisodeNfo> result = await reader.Read(stream);

            result.Count.Should().Be(2);
            result.Count(nfo => nfo.ContentRating == "US:Something").Should().Be(1);
            result.Count(nfo => nfo.ContentRating == "US:Something / US:SomethingElse").Should().Be(1);
        }

        [Test]
        public async Task No_Plot()
        {
            var reader = new EpisodeNfoReader();
            var stream = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <plot/>
</episodedetails>"));

            List<TvShowEpisodeNfo> result = await reader.Read(stream);

            result.Count.Should().Be(1);
            result[0].Plot.Should().BeNullOrEmpty();
        }

        [Test]
        public async Task Plot()
        {
            var reader = new EpisodeNfoReader();
            var stream = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!--created on whatever - comment-->
<episodedetails>
  <plot>Some Plot</plot>
</episodedetails>"));

            List<TvShowEpisodeNfo> result = await reader.Read(stream);

            result.Count.Should().Be(1);
            result[0].Plot.Should().Be("Some Plot");
        }

        [Test]
        public async Task Actors()
        {
            var reader = new EpisodeNfoReader();
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

            List<TvShowEpisodeNfo> result = await reader.Read(stream);

            result.Count.Should().Be(1);
            result[0].Actors.Count.Should().Be(2);
            result[0].Actors.Count(a => a.Name == "Name 1" && a.Role == "Role 1" && a.Thumb == "Thumb 1")
                .Should().Be(1);
            result[0].Actors.Count(a => a.Name == "Name 2" && a.Role == "Role 2" && a.Thumb == "Thumb 2")
                .Should().Be(1);
        }

        [Test]
        public async Task Writers()
        {
            var reader = new EpisodeNfoReader();
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

            List<TvShowEpisodeNfo> result = await reader.Read(stream);

            result.Count.Should().Be(2);
            result.Count(nfo => nfo.Writers.Count == 1 && nfo.Writers[0] == "Writer 1").Should().Be(1);
            result.Count(nfo => nfo.Writers.Count == 2 && nfo.Writers[0] == "Writer 2" && nfo.Writers[1] == "Writer 3")
                .Should().Be(1);
        }

        [Test]
        public async Task Directors()
        {
            var reader = new EpisodeNfoReader();
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

            List<TvShowEpisodeNfo> result = await reader.Read(stream);

            result.Count.Should().Be(2);
            result.Count(nfo => nfo.Directors.Count == 1 && nfo.Directors[0] == "Director 1").Should().Be(1);
            result.Count(
                    nfo => nfo.Directors.Count == 2 && nfo.Directors[0] == "Director 2" &&
                           nfo.Directors[1] == "Director 3")
                .Should().Be(1);
        }
    }
}
