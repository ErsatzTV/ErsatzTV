using System.Text;
using Bugsnag;
using ErsatzTV.Core.Metadata.Nfo;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Metadata.Nfo;

[TestFixture]
public class ArtistNfoReaderTests
{
    [SetUp]
    public void SetUp() => _artistNfoReader = new ArtistNfoReader(new Mock<IClient>().Object);

    private ArtistNfoReader _artistNfoReader;

    [Test]
    public async Task ParsingNfo_Should_Return_Error()
    {
        await using var stream =
            new MemoryStream(Encoding.UTF8.GetBytes(@"https://www.themoviedb.org/movie/11-star-wars"));

        Either<BaseError, ArtistNfo> result = await _artistNfoReader.Read(stream);

        result.IsLeft.Should().BeTrue();
    }

    [Test]
    public async Task MetadataNfo_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(@"<artist></artist>"));

        Either<BaseError, ArtistNfo> result = await _artistNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
    }

    [Test]
    public async Task CombinationNfo_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<artist></artist>
https://www.themoviedb.org/movie/11-star-wars"));

        Either<BaseError, ArtistNfo> result = await _artistNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
    }

    [Test]
    public async Task FullSample_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes"" ?>
<artist>
    <name>Billy Joel</name>
    <musicBrainzArtistID>64b94289-9474-4d43-8c93-918ccc1920d1</musicBrainzArtistID>
    <sortname>Joel, Billy</sortname>
    <type>Person</type>
    <gender>Male</gender>
    <disambiguation></disambiguation>
    <genre>Pop/Rock</genre>
    <style>Album Rock</style>
    <style>Contemporary Pop/Rock</style>
    <style>Singer/Songwriter</style>
    <style>Soft Rock</style>
    <style>Keyboard</style>
    <mood>Amiable/Good-Natured</mood>
    <mood>Autumnal</mood>
    <mood>Nostalgic</mood>
    <mood>Refined</mood>
    <mood>Acerbic</mood>
    <mood>Bittersweet</mood>
    <mood>Brash</mood>
    <mood>Cynical/Sarcastic</mood>
    <mood>Earnest</mood>
    <yearsactive>1960s - 2010s</yearsactive>
    <born>1949-05-09</born>
    <formed>1964</formed>
    <biography>William Martin &quot;Billy&quot; Joel (born May 9, 1949, New York, USA) is an American pianist, singer-songwriter, and composer. Since releasing his first hit song, &quot;Piano Man&quot;, in 1973, Joel has become the sixth-best-selling recording artist and the third-best-selling solo artist in the United States, according to the RIAA. His compilation album Greatest Hits Vol. 1 &amp; 2 is the third-best-selling album in the United States by discs shipped.&#x0A;Joel had Top 40 hits in the 1970s, 1980s, and 1990s, achieving 33 Top 40 hits in the United States, all of which he wrote himself. He is also a six-time Grammy Award winner, a 23-time Grammy nominee and one of the world&apos;s best-selling artists of all time, having sold over 150 million records worldwide. He was inducted into the Songwriter&apos;s Hall of Fame (1992), the Rock and Roll Hall of Fame (1999), and the Long Island Music Hall of Fame (2006). In 2008, Billboard magazine released a list of the Hot 100 All-Time Top Artists to celebrate the US singles chart&apos;s 50th anniversary, with Billy Joel positioned at No. 23. With the exception of the 2007 songs &quot;All My Life&quot; and &quot;Christmas in Fallujah&quot;, Joel stopped writing and recording popular music after 1993&apos;s River of Dreams, but he continued to tour extensively until 2010. Joel was born in the Bronx, May 9, 1949 and raised in Hicksville, New York in a Levitt home. His father, Howard (born Helmuth), was born in Germany, the son of German merchant and manufacturer Karl Amson Joel, who, after the advent of the Nazi regime, emigrated to Switzerland and later to the United States. Billy Joel&apos;s mother, Rosalind Nyman, was born in England to Philip and Rebecca Nyman. Both of Joel&apos;s parents were Jewish. They divorced in 1960, and his father moved to Vienna, Austria. Billy has a sister, Judith Joel, and a half-brother, Alexander Joel, who is an acclaimed classical conductor in Europe and currently chief musical director of the Staatstheater Braunschweig.&#x0A;Joel&apos;s father was an accomplished classical pianist. Billy reluctantly began piano lessons at an early age, at his mother&apos;s insistence; his teachers included the noted American pianist Morton Estrin and musician/songwriter Timothy Ford. His interest in music, rather than sports, was a source of teasing and bullying in his early years. (He has said in interviews that his piano instructor also taught ballet. Her name was Frances Neiman, and she was a Juilliard trained musician. She gave both classical piano and ballet lessons in the studio attached to the rear of her house, leading neighborhood bullies to mistakenly assume that he was learning to dance.) As a teenager, Joel took up boxing so that he would be able to defend himself. He boxed successfully on the amateur Golden Gloves circuit for a short time, winning twenty-two bouts, but abandoned the sport shortly after breaking his nose in his twenty-fourth boxing match.&#x0A;Joel attended Hicksville High School in 1967, but he did not graduate with his class. He had been helping his single mother make ends meet by playing at a piano bar, which interfered with his school attendance. At the end of his senior year, Joel did not have enough credits to graduate. Rather than attend summer school to earn his diploma, however, Joel decided to immediately begin a career in music. Joel recounted, &quot;I told them, &apos;To hell with it. If I&apos;m not going to Columbia University, I&apos;m going to Columbia Records, and you don&apos;t need a high school diploma over there&apos;.&quot; Columbia did, in fact, become the label that eventually signed him. In 1992, he submitted essays to the school board and was awarded his diploma at Hicksville High&apos;s annual graduation ceremony, 25 years after he had left.</biography>
    <died></died>
    <disbanded></disbanded>
    <thumb spoof="""" cache="""" aspect=""thumb"" preview=""https://assets.fanart.tv/preview/music/64b94289-9474-4d43-8c93-918ccc1920d1/artistthumb/joel-billy-541603848114c.jpg"">https://assets.fanart.tv/fanart/music/64b94289-9474-4d43-8c93-918ccc1920d1/artistthumb/joel-billy-541603848114c.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""thumb"" preview=""https://www.theaudiodb.com/images/media/artist/thumb/ttsxwr1425765041.jpg/preview"">https://www.theaudiodb.com/images/media/artist/thumb/ttsxwr1425765041.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""thumb"" preview=""https://rovimusic.rovicorp.com/image.jpg?c=73pC-Gp0OovlmiQL7Wp5Yd_M69_UI9rrJSVvWL2-yAg=&amp;f=2"">https://rovimusic.rovicorp.com/image.jpg?c=73pC-Gp0OovlmiQL7Wp5Yd_M69_UI9rrJSVvWL2-yAg=&amp;f=0</thumb>
    <thumb spoof="""" cache="""" aspect=""thumb"" preview=""https://img.discogs.com/J3bqAiLmdr2gXsetNgSQF2W-f6M=/150x150/smart/filters:strip_icc():format(jpeg):mode_rgb():quality(40)/discogs-images/A-137418-1143052539.jpeg.jpg"">https://img.discogs.com/u7cfC3lZo9JGRdukSttJTZKr9Go=/350x255/smart/filters:strip_icc():format(jpeg):mode_rgb():quality(90)/discogs-images/A-137418-1143052539.jpeg.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""clearlogo"" preview=""https://www.theaudiodb.com/images/media/artist/logo/tvqpys1367246337.png/preview"">https://www.theaudiodb.com/images/media/artist/logo/tvqpys1367246337.png</thumb>
    <thumb spoof="""" cache="""" aspect=""clearart"" preview=""https://www.theaudiodb.com/images/media/artist/clearart/yqpsuq1523892204.png/preview"">https://www.theaudiodb.com/images/media/artist/clearart/yqpsuq1523892204.png</thumb>
    <thumb spoof="""" cache="""" aspect=""landscape"" preview=""https://www.theaudiodb.com/images/media/artist/widethumb/tywpqx1530815867.jpg/preview"">https://www.theaudiodb.com/images/media/artist/widethumb/tywpqx1530815867.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""banner"" preview=""https://assets.fanart.tv/preview/music/64b94289-9474-4d43-8c93-918ccc1920d1/musicbanner/joel-billy-5914e7759bfcd.jpg"">https://assets.fanart.tv/fanart/music/64b94289-9474-4d43-8c93-918ccc1920d1/musicbanner/joel-billy-5914e7759bfcd.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""clearlogo"" preview=""https://assets.fanart.tv/preview/music/64b94289-9474-4d43-8c93-918ccc1920d1/hdmusiclogo/joel-billy-550b259604412.png"">https://assets.fanart.tv/fanart/music/64b94289-9474-4d43-8c93-918ccc1920d1/hdmusiclogo/joel-billy-550b259604412.png</thumb>
    <thumb spoof="""" cache="""" aspect=""fanart"" preview=""https://assets.fanart.tv/preview/music/64b94289-9474-4d43-8c93-918ccc1920d1/artistbackground/joel-billy-4fc0c2dad9ab7.jpg"">https://assets.fanart.tv/fanart/music/64b94289-9474-4d43-8c93-918ccc1920d1/artistbackground/joel-billy-4fc0c2dad9ab7.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""fanart"" preview=""https://www.theaudiodb.com/images/media/artist/fanart/uwqtup1521206367.jpg/preview"">https://www.theaudiodb.com/images/media/artist/fanart/uwqtup1521206367.jpg</thumb>
    <path>F:\Music\ArtistInfoKodi\Billy Joel</path>
</artist>"));

        Either<BaseError, ArtistNfo> result = await _artistNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();

        foreach (ArtistNfo nfo in result.RightToSeq())
        {
            nfo.Name.Should().Be("Billy Joel");
            nfo.Disambiguation.Should().BeNullOrEmpty();
            nfo.Genres.Should().BeEquivalentTo(new List<string> { "Pop/Rock" });
            nfo.Styles.Should().BeEquivalentTo(
                new List<string>
                {
                    "Album Rock",
                    "Contemporary Pop/Rock",
                    "Singer/Songwriter",
                    "Soft Rock",
                    "Keyboard"
                });
            nfo.Moods.Should().BeEquivalentTo(
                new List<string>
                {
                    "Amiable/Good-Natured",
                    "Autumnal",
                    "Nostalgic",
                    "Refined",
                    "Acerbic",
                    "Bittersweet",
                    "Brash",
                    "Cynical/Sarcastic",
                    "Earnest"
                });
            nfo.Biography.Should().Be(
                $@"William Martin ""Billy"" Joel (born May 9, 1949, New York, USA) is an American pianist, singer-songwriter, and composer. Since releasing his first hit song, ""Piano Man"", in 1973, Joel has become the sixth-best-selling recording artist and the third-best-selling solo artist in the United States, according to the RIAA. His compilation album Greatest Hits Vol. 1 & 2 is the third-best-selling album in the United States by discs shipped.{Environment.NewLine}Joel had Top 40 hits in the 1970s, 1980s, and 1990s, achieving 33 Top 40 hits in the United States, all of which he wrote himself. He is also a six-time Grammy Award winner, a 23-time Grammy nominee and one of the world's best-selling artists of all time, having sold over 150 million records worldwide. He was inducted into the Songwriter's Hall of Fame (1992), the Rock and Roll Hall of Fame (1999), and the Long Island Music Hall of Fame (2006). In 2008, Billboard magazine released a list of the Hot 100 All-Time Top Artists to celebrate the US singles chart's 50th anniversary, with Billy Joel positioned at No. 23. With the exception of the 2007 songs ""All My Life"" and ""Christmas in Fallujah"", Joel stopped writing and recording popular music after 1993's River of Dreams, but he continued to tour extensively until 2010. Joel was born in the Bronx, May 9, 1949 and raised in Hicksville, New York in a Levitt home. His father, Howard (born Helmuth), was born in Germany, the son of German merchant and manufacturer Karl Amson Joel, who, after the advent of the Nazi regime, emigrated to Switzerland and later to the United States. Billy Joel's mother, Rosalind Nyman, was born in England to Philip and Rebecca Nyman. Both of Joel's parents were Jewish. They divorced in 1960, and his father moved to Vienna, Austria. Billy has a sister, Judith Joel, and a half-brother, Alexander Joel, who is an acclaimed classical conductor in Europe and currently chief musical director of the Staatstheater Braunschweig.{Environment.NewLine}Joel's father was an accomplished classical pianist. Billy reluctantly began piano lessons at an early age, at his mother's insistence; his teachers included the noted American pianist Morton Estrin and musician/songwriter Timothy Ford. His interest in music, rather than sports, was a source of teasing and bullying in his early years. (He has said in interviews that his piano instructor also taught ballet. Her name was Frances Neiman, and she was a Juilliard trained musician. She gave both classical piano and ballet lessons in the studio attached to the rear of her house, leading neighborhood bullies to mistakenly assume that he was learning to dance.) As a teenager, Joel took up boxing so that he would be able to defend himself. He boxed successfully on the amateur Golden Gloves circuit for a short time, winning twenty-two bouts, but abandoned the sport shortly after breaking his nose in his twenty-fourth boxing match.{Environment.NewLine}Joel attended Hicksville High School in 1967, but he did not graduate with his class. He had been helping his single mother make ends meet by playing at a piano bar, which interfered with his school attendance. At the end of his senior year, Joel did not have enough credits to graduate. Rather than attend summer school to earn his diploma, however, Joel decided to immediately begin a career in music. Joel recounted, ""I told them, 'To hell with it. If I'm not going to Columbia University, I'm going to Columbia Records, and you don't need a high school diploma over there'."" Columbia did, in fact, become the label that eventually signed him. In 1992, he submitted essays to the school board and was awarded his diploma at Hicksville High's annual graduation ceremony, 25 years after he had left.");
        }
    }

    [Test]
    public async Task MetadataNfo_With_Disambiguation_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(@"<artist><disambiguation>Test Disambiguation</disambiguation></artist>"));

        Either<BaseError, ArtistNfo> result = await _artistNfoReader.Read(stream);

        result.IsRight.Should().BeTrue();
        foreach (ArtistNfo nfo in result.RightToSeq())
        {
            nfo.Disambiguation.Should().Be("Test Disambiguation");
        }
    }
}
