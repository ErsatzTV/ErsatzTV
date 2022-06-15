namespace ErsatzTV.FFmpeg.Capabilities;

public class NoHardwareCapabilities : IHardwareCapabilities
{
    public bool CanDecode(string videoFormat) => false;
    public bool CanEncode(string videoFormat) => false;
}
