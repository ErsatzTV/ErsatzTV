using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderMpeg2Vaapi : EncoderBase
{
    public override string Name => "mpeg2_vaapi";
    public override StreamKind Kind => StreamKind.Video;
    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Mpeg2Video
        // don't change the frame data location
    };
}
