using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class FFmpegCapabilities : IFFmpegCapabilities
{
    private readonly IReadOnlySet<string> _ffmpegDecoders;
    private readonly IReadOnlySet<string> _ffmpegFilters;
    private readonly IReadOnlySet<string> _ffmpegEncoders;

    public FFmpegCapabilities(
        IReadOnlySet<string> ffmpegDecoders,
        IReadOnlySet<string> ffmpegFilters,
        IReadOnlySet<string> ffmpegEncoders)
    {
        _ffmpegDecoders = ffmpegDecoders;
        _ffmpegFilters = ffmpegFilters;
        _ffmpegEncoders = ffmpegEncoders;
    }

    public bool HasDecoder(string decoder) => _ffmpegDecoders.Contains(decoder);

    public bool HasEncoder(string encoder) => _ffmpegEncoders.Contains(encoder);

    public bool HasFilter(string filter) => _ffmpegFilters.Contains(filter);

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
