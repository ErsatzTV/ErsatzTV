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
            var sb = new StringBuilder();
            using var xml = XmlWriter.Create(sb);
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
                    MediaMetadata metadata = Optional(playoutItem.MediaItem.Metadata).IfNone(
                        new MediaMetadata
                        {
                            Title = Path.GetFileName(playoutItem.MediaItem.Path)
                        });

                    xml.WriteStartElement("programme");
                    xml.WriteAttributeString("start", start);
                    xml.WriteAttributeString("stop", stop);
                    xml.WriteAttributeString("channel", channel.Number.ToString());

                    xml.WriteStartElement("title");
                    xml.WriteAttributeString("lang", "en");
                    xml.WriteString(metadata.Title);
                    xml.WriteEndElement(); // title
                    
                    xml.WriteStartElement("previously-shown");
                    xml.WriteEndElement(); // previously-shown

                    xml.WriteStartElement("sub-title");
                    xml.WriteAttributeString("lang", "en");
                    xml.WriteEndElement(); // sub-title

                    int season = Optional(metadata.SeasonNumber).IfNone(0);
                    int episode = Optional(metadata.EpisodeNumber).IfNone(0);
                    if (season > 0 && episode > 0)
                    {
                        xml.WriteStartElement("episode-num");
                        xml.WriteAttributeString("system", "xmltv_ns");
                        xml.WriteString($"{season - 1}.{episode - 1}.0/1");
                        xml.WriteEndElement(); // episode-num
                    }

                    // sb.AppendLine("<icon src=\"\"/>");
                    xml.WriteStartElement("desc");
                    xml.WriteAttributeString("lang", "en");
                    xml.WriteString(metadata.Description);
                    xml.WriteEndElement(); // desc

                    if (!string.IsNullOrWhiteSpace(metadata.ContentRating))
                    {
                        xml.WriteStartElement("rating");
                        xml.WriteAttributeString("system", "MPAA");
                        xml.WriteStartElement("value");
                        xml.WriteString(metadata.ContentRating);
                        xml.WriteEndElement(); // value
                        xml.WriteEndElement(); // rating
                    }

                    xml.WriteEndElement(); // programme
                }
            }

            xml.WriteEndElement(); // tv
            xml.WriteEndDocument();

            return sb.ToString();
        }
    }
}
