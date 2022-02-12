using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder;

public static class AvailableDecoders
{
    public static IDecoder ForVideoFormat(FrameState desiredState)
    {
        // TODO: hw accel?
        return desiredState.VideoFormat switch
        {
            // VideoFormat.Hevc => new EncoderLibx265(),
            VideoFormat.H264 => new DecoderH264(),
            _ => throw new ArgumentOutOfRangeException(nameof(desiredState.VideoFormat), desiredState.VideoFormat, null)
        };
    }
}
