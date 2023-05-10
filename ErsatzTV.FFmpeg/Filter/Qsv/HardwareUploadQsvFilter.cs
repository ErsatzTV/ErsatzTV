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

    public override string Filter => _currentState.FrameDataLocation switch
    {
        FrameDataLocation.Hardware => string.Empty,
        _ => $"hwupload=extra_hw_frames={_ffmpegState.QsvExtraHardwareFrames}"
    };

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Hardware };
}
