using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class FFmpegCapabilities : IFFmpegCapabilities
{
    private readonly IReadOnlySet<string> _ffmpegDecoders;
    private readonly IReadOnlySet<string> _ffmpegEncoders;
    private readonly IReadOnlySet<string> _ffmpegFilters;
    private readonly IReadOnlySet<string> _ffmpegHardwareAccelerations;
    private readonly IReadOnlySet<string> _ffmpegOptions;

    public FFmpegCapabilities(
        IReadOnlySet<string> ffmpegHardwareAccelerations,
        IReadOnlySet<string> ffmpegDecoders,
        IReadOnlySet<string> ffmpegFilters,
        IReadOnlySet<string> ffmpegEncoders,
        IReadOnlySet<string> ffmpegOptions)
    {
        _ffmpegHardwareAccelerations = ffmpegHardwareAccelerations;
        _ffmpegDecoders = ffmpegDecoders;
        _ffmpegFilters = ffmpegFilters;
        _ffmpegEncoders = ffmpegEncoders;
        _ffmpegOptions = ffmpegOptions;
    }

    public bool HasHardwareAcceleration(HardwareAccelerationMode hardwareAccelerationMode)
    {
        // AMF isn't a "hwaccel" in ffmpeg, so check for presence of encoders
        if (hardwareAccelerationMode is HardwareAccelerationMode.Amf)
        {
            return _ffmpegEncoders.Any(
                e => e.EndsWith($"_{FFmpegKnownHardwareAcceleration.Amf.Name}", StringComparison.OrdinalIgnoreCase));
        }

        Option<FFmpegKnownHardwareAcceleration> maybeAccelToCheck = hardwareAccelerationMode switch
        {
            HardwareAccelerationMode.Nvenc => FFmpegKnownHardwareAcceleration.Cuda,
            HardwareAccelerationMode.Qsv => FFmpegKnownHardwareAcceleration.Qsv,
            HardwareAccelerationMode.Vaapi => FFmpegKnownHardwareAcceleration.Vaapi,
            HardwareAccelerationMode.VideoToolbox => FFmpegKnownHardwareAcceleration.VideoToolbox,
            _ => Option<FFmpegKnownHardwareAcceleration>.None
        };

        foreach (FFmpegKnownHardwareAcceleration accelToCheck in maybeAccelToCheck)
        {
            return _ffmpegHardwareAccelerations.Contains(accelToCheck.Name);
        }

        return false;
    }

    public bool HasDecoder(FFmpegKnownDecoder decoder) => _ffmpegDecoders.Contains(decoder.Name);

    public bool HasEncoder(FFmpegKnownEncoder encoder) => _ffmpegEncoders.Contains(encoder.Name);

    public bool HasFilter(FFmpegKnownFilter filter) => _ffmpegFilters.Contains(filter.Name);

    public bool HasOption(FFmpegKnownOption ffmpegOption) => _ffmpegOptions.Contains(ffmpegOption.Name);

    public Option<IDecoder> SoftwareDecoderForVideoFormat(string videoFormat) =>
        videoFormat switch
        {
            VideoFormat.Hevc => new DecoderHevc(),
            VideoFormat.H264 => new DecoderH264(),
            VideoFormat.Mpeg1Video => new DecoderMpeg1Video(),
            VideoFormat.Mpeg2Video => new DecoderMpeg2Video(),
            VideoFormat.Vc1 => new DecoderVc1(),
            VideoFormat.MsMpeg4V2 => new DecoderMsMpeg4V2(),
            VideoFormat.MsMpeg4V3 => new DecoderMsMpeg4V3(),
            VideoFormat.Mpeg4 => new DecoderMpeg4(),
            VideoFormat.Vp9 => new DecoderVp9(),
            VideoFormat.Av1 => new DecoderAv1(_ffmpegDecoders),

            VideoFormat.Undetermined => new DecoderImplicit(),
            VideoFormat.Copy => new DecoderImplicit(),
            VideoFormat.GeneratedImage => new DecoderImplicit(),

            _ => Option<IDecoder>.None
        };
}
