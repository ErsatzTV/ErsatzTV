using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderH264Vaapi : EncoderBase
{
    public override string Name => "h264_vaapi";
    public override StreamKind Kind => StreamKind.Video;
    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264
        // don't change the frame data location
    };
}
