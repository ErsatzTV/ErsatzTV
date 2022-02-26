namespace ErsatzTV.FFmpeg.Filter;

public class HardwareUploadFilter : BaseFilter
{
    private readonly FFmpegState _ffmpegState;

    public HardwareUploadFilter(FFmpegState ffmpegState)
    {
        _ffmpegState = ffmpegState;
    }

    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter => _ffmpegState.HardwareAccelerationMode switch
    {
        HardwareAccelerationMode.None => string.Empty,
        HardwareAccelerationMode.Nvenc => "hwupload_cuda",
        HardwareAccelerationMode.Qsv => "hwupload=extra_hw_frames=64",
        _ => "hwupload"
    };
}
