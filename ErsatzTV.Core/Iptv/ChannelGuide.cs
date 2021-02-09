using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<tv generator-info-name=\"ersatztv\">");

            foreach (Channel channel in _channels)
            {
                sb.AppendLine($"<channel id=\"{channel.Number}\">");
                sb.AppendLine($"<display-name lang=\"en\">{channel.Name}</display-name>");
                sb.AppendLine(
                    !string.IsNullOrWhiteSpace(channel.Logo)
                        ? $"<icon src=\"{_scheme}://{_host}/iptv/images/{channel.Logo}\"/>"
                        : $"<icon src=\"{_scheme}://{_host}/images/ersatztv-500.png\"/>");

                sb.AppendLine("</channel>");
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

                    sb.AppendLine(
                        $"<programme start=\"{start}\" stop=\"{stop}\" channel=\"{channel.Number}\">");
                    sb.AppendLine($"<title lang=\"en\">{metadata.Title}</title>");
                    sb.AppendLine("<previously-shown/>");
                    sb.AppendLine("<sub-title lang=\"en\"></sub-title>");

                    int season = Optional(metadata.SeasonNumber).IfNone(0);
                    int episode = Optional(metadata.EpisodeNumber).IfNone(0);
                    if (season > 0 && episode > 0)
                    {
                        sb.AppendLine($"<episode-num system=\"xmltv_ns\">{season - 1}.{episode - 1}.0/1</episode-num>");
                    }

                    // sb.AppendLine("<icon src=\"\"/>");
                    sb.AppendLine($"<desc lang=\"en\">{metadata.Description}</desc>");

                    if (!string.IsNullOrWhiteSpace(metadata.ContentRating))
                    {
                        sb.AppendLine("<rating system=\"MPAA\">");
                        sb.AppendLine($"<value>{metadata.ContentRating}</value>");
                        sb.AppendLine("</rating>");
                    }

                    sb.AppendLine("</programme>");
                }
            }

            sb.AppendLine("</tv>");


            return sb.ToString();
        }
    }
}
