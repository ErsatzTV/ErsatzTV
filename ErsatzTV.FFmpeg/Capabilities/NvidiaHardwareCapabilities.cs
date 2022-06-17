using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class NvidiaHardwareCapabilities : IHardwareCapabilities
{
    private readonly int _architecture;

    public NvidiaHardwareCapabilities(int architecture) => _architecture = architecture;

    public bool CanDecode(string videoFormat, Option<IPixelFormat> maybePixelFormat) =>
        videoFormat switch
        {
            // second gen maxwell is required to decode hevc/vp9
            VideoFormat.Hevc or VideoFormat.Vp9 => _architecture >= 52,
            _ => true
        };

    public bool CanEncode(string videoFormat, Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        return videoFormat switch
        {
            // pascal is required to encode 10-bit hevc
            VideoFormat.Hevc when bitDepth == 10 => _architecture >= 60,

            // second gen maxwell is required to encode hevc
            VideoFormat.Hevc => _architecture >= 52,

            // nvidia cannot encode 10-bit h264
            VideoFormat.H264 when bitDepth == 10 => false,
            
            _ => true
        };
    }
}
