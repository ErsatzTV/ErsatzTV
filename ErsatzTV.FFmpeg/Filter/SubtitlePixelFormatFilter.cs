namespace ErsatzTV.FFmpeg.Filter;

public class SubtitlePixelFormatFilter : BaseFilter
{
    private readonly FFmpegState _ffmpegState;

    public SubtitlePixelFormatFilter(FFmpegState ffmpegState)
    {
        _ffmpegState = ffmpegState;
    }

    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter
    {
        get
        {
            Option<string> maybeFormat = _ffmpegState.HardwareAccelerationMode switch
            {
                HardwareAccelerationMode.Nvenc => "yuva420p",
                HardwareAccelerationMode.Qsv => "yuva420p",
                _ => None
            };

            return maybeFormat.Match(f => $"format={f}", () => string.Empty);
        }
    }
}
