using ErsatzTV.FFmpeg.Format;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class NvidiaHardwareCapabilities : IHardwareCapabilities
{
    private readonly int _architecture;
    private readonly List<string> _maxwellGm206 = new() { "GTX 750", "GTX 950", "GTX 960", "GTX 965M" };
    private readonly string _model;
    private readonly IFFmpegCapabilities _ffmpegCapabilities;
    private readonly ILogger _logger;

    public NvidiaHardwareCapabilities(
        int architecture,
        string model,
        IFFmpegCapabilities ffmpegCapabilities,
        ILogger logger)
    {
        _architecture = architecture;
        _model = model;
        _ffmpegCapabilities = ffmpegCapabilities;
        _logger = logger;
    }

    public FFmpegCapability CanDecode(string videoFormat, Option<string> videoProfile, Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        bool isHardware = videoFormat switch
        {
            // some second gen maxwell can decode hevc, otherwise pascal is required
            VideoFormat.Hevc => _architecture == 52 && _maxwellGm206.Contains(_model) || _architecture >= 60,

            // pascal is required to decode vp9 10-bit
            VideoFormat.Vp9 when bitDepth == 10 => _architecture >= 60,

            // some second gen maxwell can decode vp9, otherwise pascal is required
            VideoFormat.Vp9 => _architecture == 52 && _maxwellGm206.Contains(_model) || _architecture >= 60,
            
            // no hardware decoding of 10-bit h264
            VideoFormat.H264 when bitDepth == 10 => false,
            
            VideoFormat.Mpeg2Video => true,
            
            VideoFormat.Vc1 => true,
            
            // too many issues with odd mpeg4 content, so use software
            VideoFormat.Mpeg4 => false,
            
            // ampere is required for av1 decoding
            VideoFormat.Av1 => _architecture >= 86,

            // generated images are decoded into software
            VideoFormat.GeneratedImage => false,
            
            _ => false
        };

        if (isHardware)
        {
            return videoFormat switch
            {
                VideoFormat.Mpeg2Video => CheckHardwareCodec("mpeg2_cuvid", _ffmpegCapabilities.HasDecoder),
                VideoFormat.Mpeg4 => CheckHardwareCodec("mpeg4_cuvid", _ffmpegCapabilities.HasDecoder),
                VideoFormat.Vc1 => CheckHardwareCodec("vc1_cuvid", _ffmpegCapabilities.HasDecoder),
                VideoFormat.H264 => CheckHardwareCodec("h264_cuvid", _ffmpegCapabilities.HasDecoder),
                VideoFormat.Hevc => CheckHardwareCodec("hevc_cuvid", _ffmpegCapabilities.HasDecoder),
                VideoFormat.Vp9 => CheckHardwareCodec("hevc_cuvid", _ffmpegCapabilities.HasDecoder),
                VideoFormat.Av1 => CheckHardwareCodec("av1_cuvid", _ffmpegCapabilities.HasDecoder),
                _ => FFmpegCapability.Software
            };
        }

        return FFmpegCapability.Software;
    }

    public FFmpegCapability CanEncode(string videoFormat, Option<string> videoProfile, Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        bool isHardware = videoFormat switch
        {
            // pascal is required to encode 10-bit hevc
            VideoFormat.Hevc when bitDepth == 10 => _architecture >= 60,

            // second gen maxwell is required to encode hevc
            VideoFormat.Hevc => _architecture >= 52,

            // nvidia cannot encode 10-bit h264
            VideoFormat.H264 when bitDepth == 10 => false,

            _ => true
        };

        return isHardware ? FFmpegCapability.Hardware : FFmpegCapability.Software;
    }

    // this fails with some 1650 cards, so let's try greater than 75
    public bool HevcBFrames => _architecture > 75;

    private FFmpegCapability CheckHardwareCodec(string codec, Func<string, bool> check)
    {
        if (check(codec))
        {
            return FFmpegCapability.Hardware;
        }

        _logger.LogWarning("FFmpeg does not contain codec {Codec}; will fall back to software codec", codec);
        return FFmpegCapability.Software;
    }
}
