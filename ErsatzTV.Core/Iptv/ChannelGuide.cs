using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Iptv
{
    public class ChannelGuide
    {
        private readonly List<Channel> _channels;
        private readonly string _host;
        private readonly string _scheme;

        public ChannelGuide(string scheme, string host, List<Channel> channels)
        {
            _scheme = scheme;
            _host = host;
            _channels = channels;
        }

        public string ToXml()
        {
            using var ms = new MemoryStream();
            using var xml = XmlWriter.Create(ms);
            xml.WriteStartDocument();

            xml.WriteStartElement("tv");
            xml.WriteAttributeString("generator-info-name", "ersatztv");

            foreach (Channel channel in _channels)
            {
                xml.WriteStartElement("channel");
                xml.WriteAttributeString("id", channel.Number.ToString());

                xml.WriteStartElement("display-name");
                xml.WriteAttributeString("lang", "en");
                xml.WriteString(channel.Name);
                xml.WriteEndElement(); // display-name

                xml.WriteStartElement("icon");
                xml.WriteAttributeString(
                    "src",
                    !string.IsNullOrWhiteSpace(channel.Logo)
                        ? $"{_scheme}://{_host}/iptv/images/{channel.Logo}"
                        : $"{_scheme}://{_host}/images/ersatztv-500.png");
                xml.WriteEndElement(); // icon

                xml.WriteEndElement(); // channel
            }

            foreach (Channel channel in _channels)
            {
                foreach (PlayoutItem playoutItem in channel.Playouts.Collect(p => p.Items).OrderBy(i => i.Start))
                {
                    string start = playoutItem.Start.ToString("yyyyMMddHHmmss zzz").Replace(":", string.Empty);
                    string stop = playoutItem.Finish.ToString("yyyyMMddHHmmss zzz").Replace(":", string.Empty);

                    string title = playoutItem.MediaItem switch
                    {
                        Movie m => m.MovieMetadata.HeadOrNone().Match(mm => mm.Title, () => m.Path),
                        Episode e => e.EpisodeMetadata.HeadOrNone().Match(em => em.Title, () => e.Path),
                        _ => "[unknown]"
                    };

                    string description = playoutItem.MediaItem switch
                    {
                        Movie m => m.MovieMetadata.HeadOrNone().Map(mm => mm.Plot).IfNone(string.Empty),
                        Episode e => e.EpisodeMetadata.HeadOrNone().Map(em => em.Plot).IfNone(string.Empty),
                        _ => string.Empty
                    };

                    string contentRating = playoutItem.MediaItem switch
                    {
                        // TODO: re-implement content rating
                        // Movie m => m.MovieMetadata.HeadOrNone().Map(mm => mm.ContentRating).IfNone(string.Empty),
                        _ => string.Empty
                    };

                    xml.WriteStartElement("programme");
                    xml.WriteAttributeString("start", start);
                    xml.WriteAttributeString("stop", stop);
                    xml.WriteAttributeString("channel", channel.Number.ToString());

                    xml.WriteStartElement("title");
                    xml.WriteAttributeString("lang", "en");
                    xml.WriteString(title);
                    xml.WriteEndElement(); // title

                    xml.WriteStartElement("previously-shown");
                    xml.WriteEndElement(); // previously-shown

                    xml.WriteStartElement("sub-title");
                    xml.WriteAttributeString("lang", "en");
                    xml.WriteEndElement(); // sub-title

                    if (playoutItem.MediaItem is Episode episode)
                    {
                        int s = Optional(episode.Season?.SeasonNumber).IfNone(0);
                        int e = episode.EpisodeNumber;
                        if (s > 0 && e > 0)
                        {
                            xml.WriteStartElement("episode-num");
                            xml.WriteAttributeString("system", "xmltv_ns");
                            xml.WriteString($"{s - 1}.{e - 1}.0/1");
                            xml.WriteEndElement(); // episode-num
                        }
                    }

                    // sb.AppendLine("<icon src=\"\"/>");

                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        xml.WriteStartElement("desc");
                        xml.WriteAttributeString("lang", "en");
                        xml.WriteString(description);
                        xml.WriteEndElement(); // desc
                    }

                    if (!string.IsNullOrWhiteSpace(contentRating))
                    {
                        xml.WriteStartElement("rating");
                        xml.WriteAttributeString("system", "MPAA");
                        xml.WriteStartElement("value");
                        xml.WriteString(contentRating);
                        xml.WriteEndElement(); // value
                        xml.WriteEndElement(); // rating
                    }

                    xml.WriteEndElement(); // programme
                }
            }

            xml.WriteEndElement(); // tv
            xml.WriteEndDocument();

            xml.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
