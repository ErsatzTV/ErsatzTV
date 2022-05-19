namespace ErsatzTV.Core.Errors;

public class ChannelSessionAlreadyActive : BaseError
{
    public ChannelSessionAlreadyActive() : base("Channel already has HLS session")
    {
    }
}
