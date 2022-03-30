namespace ErsatzTV.FFmpeg.Filter;

public class SubtitleHardwareUploadFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FFmpegState _ffmpegState;

    public SubtitleHardwareUploadFilter(FrameState currentState, FFmpegState ffmpegState)
    {
        _currentState = currentState;
        _ffmpegState = ffmpegState;
    }

    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter =>
        _ffmpegState.HardwareAccelerationMode switch
        {
            HardwareAccelerationMode.None => string.Empty,
            HardwareAccelerationMode.Nvenc => "hwupload_cuda",
            HardwareAccelerationMode.Qsv => "hwupload=extra_hw_frames=64",

            // leave vaapi in software since we don't (yet) use overlay_vaapi
            HardwareAccelerationMode.Vaapi when _currentState.FrameDataLocation == FrameDataLocation.Software =>
                string.Empty,

            // leave videotoolbox in software since we use a software overlay filter
            HardwareAccelerationMode.VideoToolbox => string.Empty,

            _ => "hwupload"
        };
}
