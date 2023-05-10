using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderHevcVaapi : EncoderBase
{
    public override string Name => "hevc_vaapi";
    public override StreamKind Kind => StreamKind.Video;

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc
        // don't change the frame data location
    };
}
