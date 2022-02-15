using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.VideoToolbox;

public class EncoderHevcVideoToolbox : EncoderBase
{
    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc,
        FrameDataLocation = FrameDataLocation.Hardware
    };

    public override string Name => "hevc_videotoolbox";
    public override StreamKind Kind => StreamKind.Video;
}
