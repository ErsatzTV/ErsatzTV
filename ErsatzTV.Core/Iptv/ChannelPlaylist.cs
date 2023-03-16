using System.Text;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Iptv;

public class ChannelPlaylist
{
    private readonly List<Channel> _channels;
    private readonly string _host;
    private readonly string _baseUrl;
    private readonly string _scheme;
    private readonly string _accessToken;

    public ChannelPlaylist(string scheme, string host, string baseUrl, List<Channel> channels, string accessToken)
    {
        _scheme = scheme;
        _host = host;
        _baseUrl = baseUrl;
        _channels = channels;
        _accessToken = accessToken;
    }

    public string ToM3U()
    {
        var sb = new StringBuilder();
        
        var accessTokenUri = "";
        var accessTokenUriAmp = "";
        if (_accessToken != null)
        {
            accessTokenUri = $"?access_token={_accessToken}";
            accessTokenUriAmp = $"&access_token={_accessToken}";
        }

        string xmltv = $"{_scheme}://{_host}{_baseUrl}/iptv/xmltv.xml" + accessTokenUri;
        sb.AppendLine($"#EXTM3U url-tvg=\"{xmltv}\" x-tvg-url=\"{xmltv}\"");
        foreach (Channel channel in _channels.OrderBy(c => decimal.Parse(c.Number)))
        {
            string logo = Optional(channel.Artwork).Flatten()
                .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                .HeadOrNone()
                .Match(
                    artwork => $"{_scheme}://{_host}{_baseUrl}/iptv/logos/{artwork.Path}.jpg" + accessTokenUri,
                    () => $"{_scheme}://{_host}{_baseUrl}/iptv/images/ersatztv-500.png" + accessTokenUri);

            string shortUniqueId = Convert.ToBase64String(channel.UniqueId.ToByteArray())
                .TrimEnd('=')
                .Replace("/", "_")
                .Replace("+", "-");

            string format = channel.StreamingMode switch
            {
                StreamingMode.HttpLiveStreamingDirect => $"m3u8?mode=hls-direct" + accessTokenUriAmp,
                StreamingMode.HttpLiveStreamingSegmenter => $"m3u8?mode=segmenter" + accessTokenUriAmp,
                StreamingMode.TransportStreamHybrid => $"ts" + accessTokenUri,
                _ => $"ts?mode=ts-legacy" + accessTokenUriAmp
            };

            string vcodec = channel.FFmpegProfile.VideoFormat.ToString().ToLowerInvariant();
            string acodec = channel.FFmpegProfile.AudioFormat.ToString().ToLowerInvariant();

            sb.AppendLine(
                $"#EXTINF:0 tvg-id=\"{channel.Number}.etv\" channel-id=\"{shortUniqueId}\" channel-number=\"{channel.Number}\" CUID=\"{shortUniqueId}\" tvg-chno=\"{channel.Number}\" tvg-name=\"{channel.Name}\" tvg-logo=\"{logo}\" group-title=\"{channel.Group}\" tvc-stream-vcodec=\"{vcodec}\" tvc-stream-acodec=\"{acodec}\", {channel.Name}");
            sb.AppendLine($"{_scheme}://{_host}{_baseUrl}/iptv/channel/{channel.Number}.{format}");
        }

        return sb.ToString();
    }
}
