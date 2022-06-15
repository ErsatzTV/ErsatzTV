namespace ErsatzTV.FFmpeg.Capabilities;

public class DefaultHardwareCapabilities : IHardwareCapabilities
{
    public bool CanDecode(string videoFormat) => true;
    public bool CanEncode(string videoFormat) => true;
}
