namespace ErsatzTV.Application.Streaming.Queries
{
    public record GetPlayoutItemProcessByChannelNumber : FFmpegProcessRequest
    {
        public GetPlayoutItemProcessByChannelNumber(int channelNumber) : base(channelNumber)
        {
        }
    }
}
