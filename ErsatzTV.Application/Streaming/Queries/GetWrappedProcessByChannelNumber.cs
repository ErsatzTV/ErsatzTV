using System;

namespace ErsatzTV.Application.Streaming.Queries
{
    public record GetWrappedProcessByChannelNumber : FFmpegProcessRequest
    {
        public GetWrappedProcessByChannelNumber(string scheme, string host, string channelNumber) : base(
            channelNumber,
            "ts",
            DateTimeOffset.Now,
            false,
            true)
        {
            Scheme = scheme;
            Host = host;
        }

        public string Scheme { get; }
        public string Host { get; }
    }
}
