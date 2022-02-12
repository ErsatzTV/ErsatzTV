using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class AvailableEncoders
{
    public static IEncoder ForVideoFormat(FrameState desiredState)
    {
        // TODO: hw accel?
        return desiredState.VideoFormat switch
        {
            VideoFormat.Hevc => new EncoderLibx265(),
            VideoFormat.H264 => new EncoderLibx264(),
            _ => throw new ArgumentOutOfRangeException(nameof(desiredState.VideoFormat), desiredState.VideoFormat, null)
        };
    }
}
