using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class NvidiaHardwareCapabilities : IHardwareCapabilities
{
    private readonly int _architecture;
    private readonly List<string> _maxwellGm206 = new() { "GTX 750", "GTX 950", "GTX 960", "GTX 965M" };
    private readonly string _model;

    public NvidiaHardwareCapabilities(int architecture, string model)
    {
        _architecture = architecture;
        _model = model;
    }

    public bool CanDecode(string videoFormat, Option<string> videoProfile, Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        return videoFormat switch
        {
            // some second gen maxwell can decode hevc, otherwise pascal is required
            VideoFormat.Hevc => _architecture == 52 && _maxwellGm206.Contains(_model) || _architecture >= 60,

            // pascal is required to decode vp9 10-bit
            VideoFormat.Vp9 when bitDepth == 10 => _architecture >= 60,

            // some second gen maxwell can decode vp9, otherwise pascal is required
            VideoFormat.Vp9 => _architecture == 52 && _maxwellGm206.Contains(_model) || _architecture >= 60,
            
            // no hardware decoding of 10-bit h264
            VideoFormat.H264 when bitDepth == 10 => false,
            
            // generated images are decoded into software
            VideoFormat.GeneratedImage => false,

            _ => true
        };
    }

    public bool CanEncode(string videoFormat, Option<string> videoProfile, Option<IPixelFormat> maybePixelFormat)
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
