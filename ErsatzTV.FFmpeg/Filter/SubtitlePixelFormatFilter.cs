namespace ErsatzTV.FFmpeg.Filter;

public class SubtitlePixelFormatFilter : BaseFilter
{
    private readonly FFmpegState _ffmpegState;

    public SubtitlePixelFormatFilter(FFmpegState ffmpegState) => _ffmpegState = ffmpegState;

    public override string Filter => MaybeFormat.Match(f => $"format={f}", () => string.Empty);

    public Option<string> MaybeFormat =>_ffmpegState.EncoderHardwareAccelerationMode switch
    {
        HardwareAccelerationMode.Nvenc => "yuva420p",
        HardwareAccelerationMode.Qsv => "yuva420p",
        _ => None
    }; 

    public override FrameState NextState(FrameState currentState) => currentState;
}
