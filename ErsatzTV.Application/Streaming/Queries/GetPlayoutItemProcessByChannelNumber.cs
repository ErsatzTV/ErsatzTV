using System;

namespace ErsatzTV.Application.Streaming.Queries
{
    public record GetPlayoutItemProcessByChannelNumber : FFmpegProcessRequest
    {
        public GetPlayoutItemProcessByChannelNumber(
            string channelNumber,
            string mode,
            DateTimeOffset now,
            bool startAtZero,
            bool hlsRealtime) : base(
            channelNumber,
            mode,
            now,
            startAtZero,
            hlsRealtime)
        {
        }
    }
}
