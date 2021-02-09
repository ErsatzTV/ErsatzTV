using System;
using System.Collections.Generic;
using System.Text;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Iptv
{
    public class ChannelPlaylist
    {
        private readonly List<Channel> _channels;
        private readonly string _host;
        private readonly string _scheme;

        public ChannelPlaylist(string scheme, string host, List<Channel> channels)
        {
            _scheme = scheme;
            _host = host;
            _channels = channels;
        }

        public string ToM3U()
        {
            var sb = new StringBuilder();

            var xmltv = $"{_scheme}://{_host}/iptv/xmltv.xml";
            sb.AppendLine($"#EXTM3U url-tvg=\"{xmltv}\" x-tvg-url=\"{xmltv}\"");
            foreach (Channel channel in _channels)
            {
                string logo = !string.IsNullOrWhiteSpace(channel.Logo)
                    ? $"{_scheme}://{_host}/iptv/images/{channel.Logo}"
                    : $"{_scheme}://{_host}/images/ersatztv-500.png";

                string shortUniqueId = Convert.ToBase64String(channel.UniqueId.ToByteArray())
                    .TrimEnd('=')
                    .Replace("/", "_")
                    .Replace("+", "-");

                string format = channel.StreamingMode switch
                {
                    StreamingMode.HttpLiveStreaming => "m3u8",
                    _ => "ts"
                };

                sb.AppendLine(
                    $"#EXTINF:0 tvg-id=\"{channel.Number}\" CUID=\"{shortUniqueId}\" tvg-chno=\"{channel.Number}\" tvg-name=\"{channel.Name}\" tvg-logo=\"{logo}\" group-title=\"ErsatzTV\", {channel.Name}");
                sb.AppendLine($"{_scheme}://{_host}/iptv/channel/{channel.Number}.{format}");
            }

            return sb.ToString();
        }
    }
}
