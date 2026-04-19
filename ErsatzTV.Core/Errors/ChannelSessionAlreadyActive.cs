namespace ErsatzTV.Core.Errors;

public class ChannelSessionAlreadyActive(string multiVariantPlaylist) : BaseError("Channel already has HLS session")
{
    public string MultiVariantPlaylist { get; } = multiVariantPlaylist;
}
