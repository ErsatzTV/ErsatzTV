using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class NvidiaHardwareCapabilities : IHardwareCapabilities
{
    private readonly int _architecture;

    public NvidiaHardwareCapabilities(int architecture) => _architecture = architecture;

    public bool CanDecode(string videoFormat) =>
        videoFormat switch
        {
            // pascal is required to decode hevc/vp9
            VideoFormat.Hevc or VideoFormat.Vp9 => _architecture >= 60,
            _ => true
        };

    public bool CanEncode(string videoFormat) =>
        videoFormat switch
        {
            // pascal is required to encode hevc
            VideoFormat.Hevc => _architecture >= 60,
            _ => true
        };
}
