namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class HardwareUploadQsvFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FFmpegState _ffmpegState;

    public HardwareUploadQsvFilter(FrameState currentState, FFmpegState ffmpegState)
    {
        _currentState = currentState;
        _ffmpegState = ffmpegState;
    }

    public override string Filter => _currentState.FrameDataLocation is FrameDataLocation.Software
        ? $"hwupload=extra_hw_frames={_ffmpegState.QsvExtraHardwareFrames},format=qsv"
        : string.Empty;

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Hardware };
}
