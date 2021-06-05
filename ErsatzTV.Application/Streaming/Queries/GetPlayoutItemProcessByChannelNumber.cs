namespace ErsatzTV.Application.Streaming.Queries
{
    public record GetPlayoutItemProcessByChannelNumber : FFmpegProcessRequest
    {
        public GetPlayoutItemProcessByChannelNumber(string channelNumber, string mode) : base(channelNumber, mode)
        {
        }
    }
}
