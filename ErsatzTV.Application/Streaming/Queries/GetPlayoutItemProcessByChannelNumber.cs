namespace ErsatzTV.Application.Streaming.Queries
{
    public record GetPlayoutItemProcessByChannelNumber : FFmpegProcessRequest
    {
        public GetPlayoutItemProcessByChannelNumber(string channelNumber, string mode, bool startAtZero) : base(
            channelNumber,
            mode,
            startAtZero)
        {
        }
    }
}
