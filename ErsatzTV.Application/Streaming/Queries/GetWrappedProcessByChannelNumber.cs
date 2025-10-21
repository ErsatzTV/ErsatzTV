using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Streaming;

public record GetWrappedProcessByChannelNumber : FFmpegProcessRequest
{
    public GetWrappedProcessByChannelNumber(
        string scheme,
        string host,
        string accessToken,
        string channelNumber) : base(
        channelNumber,
        StreamingMode.TransportStreamHybrid,
        DateTimeOffset.Now,
        false,
        true,
        DateTimeOffset.Now, // unused
        TimeSpan.Zero)
    {
        Scheme = scheme;
        Host = host;
        AccessToken = accessToken;
    }

    public string Scheme { get; }
    public string Host { get; }
    public string AccessToken { get; }
}
