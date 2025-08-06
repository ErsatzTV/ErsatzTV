namespace ErsatzTV.Application.Streaming;

public record GetWrappedProcessByChannelNumber : FFmpegProcessRequest
{
    public GetWrappedProcessByChannelNumber(
        string scheme,
        string host,
        string accessToken,
        string channelNumber) : base(
        channelNumber,
        "ts",
        DateTimeOffset.Now,
        false,
        true,
        DateTimeOffset.Now, // unused
        0)
    {
        Scheme = scheme;
        Host = host;
        AccessToken = accessToken;
    }

    public string Scheme { get; }
    public string Host { get; }
    public string AccessToken { get; }
}
