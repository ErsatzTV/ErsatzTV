namespace ErsatzTV.Application.Streaming.Queries
{
    public record GetConcatProcessByChannelNumber : FFmpegProcessRequest
    {
        public GetConcatProcessByChannelNumber(string scheme, string host, string channelNumber) : base(
            channelNumber,
            "ts",
            false)
        {
            Scheme = scheme;
            Host = host;
        }

        public string Scheme { get; }
        public string Host { get; }
    }
}
