namespace ErsatzTV.Application.Streaming.Queries
{
    public record GetPlayoutItemProcessByChannelNumber : FFmpegProcessRequest
    {
        public GetPlayoutItemProcessByChannelNumber(string channelNumber) : base(channelNumber)
        {
        }
    }
}
