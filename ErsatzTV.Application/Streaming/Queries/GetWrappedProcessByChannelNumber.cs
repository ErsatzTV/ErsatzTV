namespace ErsatzTV.Application.Streaming;

public record GetWrappedProcessByChannelNumber : FFmpegProcessRequest
{
    public GetWrappedProcessByChannelNumber(string scheme, string host, string channelNumber) : base(
        channelNumber,
        "ts",
        DateTimeOffset.Now,
        false,
        true,
        0)
    {
        Scheme = scheme;
        Host = host;
    }

    public string Scheme { get; }
    public string Host { get; }
}