using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderHevcVaapi : EncoderBase
{
    private readonly FrameState _currentState;

    public EncoderHevcVaapi(FrameState currentState)
    {
        _currentState = currentState;
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc,
        FrameDataLocation = FrameDataLocation.Hardware
    };

    public override string Name => "hevc_vaapi";
    public override StreamKind Kind => StreamKind.Video;

    public override string Filter => _currentState.FrameDataLocation == FrameDataLocation.Software
        ? "format=nv12|vaapi,hwupload"
        : string.Empty;
}
