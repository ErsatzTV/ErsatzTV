using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

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
            foreach (Channel channel in _channels.OrderBy(c => c.Number))
            {
                string logo = Optional(channel.Artwork).Flatten()
                    .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                    .HeadOrNone()
                    .Match(
                        artwork => $"{_scheme}://{_host}/iptv/logos/{artwork.Path}",
                        () => $"{_scheme}://{_host}/iptv/images/ersatztv-500.png");

                string shortUniqueId = Convert.ToBase64String(channel.UniqueId.ToByteArray())
                    .TrimEnd('=')
                    .Replace("/", "_")
                    .Replace("+", "-");

                string format = channel.StreamingMode switch
                {
                    StreamingMode.HttpLiveStreamingDirect => "m3u8",
                    _ => "ts"
                };

                string vcodec = channel.FFmpegProfile.VideoCodec.Split("_").Head();
                string acodec = channel.FFmpegProfile.AudioCodec;

                sb.AppendLine(
                    $"#EXTINF:0 tvg-id=\"{channel.Number}\" channel-id=\"{shortUniqueId}\" channel-number=\"{channel.Number}\" CUID=\"{shortUniqueId}\" tvg-chno=\"{channel.Number}\" tvg-name=\"{channel.Name}\" tvg-logo=\"{logo}\" group-title=\"ErsatzTV\" tvc-stream-vcodec=\"{vcodec}\" tvc-stream-acodec=\"{acodec}\", {channel.Name}");
                sb.AppendLine($"{_scheme}://{_host}/iptv/channel/{channel.Number}.{format}");
            }

            return sb.ToString();
        }
    }
}
