namespace ErsatzTV.FFmpeg.Filter;

public class SubtitlePixelFormatFilter : BaseFilter
{
    private readonly FFmpegState _ffmpegState;
    private readonly bool _is10BitOutput;

    public SubtitlePixelFormatFilter(FFmpegState ffmpegState, bool is10BitOutput)
    {
        _ffmpegState = ffmpegState;
        _is10BitOutput = is10BitOutput;
    }

    public override string Filter => MaybeFormat.Match(f => $"format={f}", () => string.Empty);

    public Option<string> MaybeFormat => _ffmpegState.EncoderHardwareAccelerationMode switch
    {
        HardwareAccelerationMode.Nvenc when _is10BitOutput => "nv12",
        HardwareAccelerationMode.Nvenc => "yuva420p",
        HardwareAccelerationMode.Qsv => "yuva420p",
        _ => None
    };

    public override FrameState NextState(FrameState currentState) => currentState;
}
