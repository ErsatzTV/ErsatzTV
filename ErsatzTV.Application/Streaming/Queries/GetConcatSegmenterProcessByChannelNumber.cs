using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Streaming;

public record GetConcatSegmenterProcessByChannelNumber : FFmpegProcessRequest
{
    public GetConcatSegmenterProcessByChannelNumber(string scheme, string host, string channelNumber) : base(
        channelNumber,
        StreamingMode.TransportStream,
        DateTimeOffset.Now,
        false,
        true,
        DateTimeOffset.Now, // unused
        0)
    {
        Scheme = scheme;
        Host = host;
    }

    public string Scheme { get; }
    public string Host { get; }
}
