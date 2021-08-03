namespace ErsatzTV.Core.Errors
{
    public class ChannelHasProcess : BaseError
    {
        public ChannelHasProcess() : base("Channel already has ffmpeg process")
        {
        }
    }
}
