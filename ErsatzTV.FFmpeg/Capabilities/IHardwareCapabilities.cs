namespace ErsatzTV.FFmpeg.Capabilities;

public interface IHardwareCapabilities
{
    public bool CanDecode(string videoFormat);
    public bool CanEncode(string videoFormat);
}
