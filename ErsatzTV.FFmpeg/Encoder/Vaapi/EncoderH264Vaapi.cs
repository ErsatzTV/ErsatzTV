using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderH264Vaapi : EncoderBase
{
    private readonly FrameState _currentState;

    public EncoderH264Vaapi(FrameState currentState)
    {
        _currentState = currentState;
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };

    public override string Name => "h264_vaapi";
    public override StreamKind Kind => StreamKind.Video;

    public override string Filter => _currentState.FrameDataLocation == FrameDataLocation.Software
        ? "hwupload"
        : string.Empty;
}
