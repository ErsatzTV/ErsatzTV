using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public class WatermarkPixelFormatFilter : BaseFilter
{
    private readonly FFmpegState _ffmpegState;
    private readonly WatermarkState _watermarkState;
    private readonly bool _is10BitOutput;

    public WatermarkPixelFormatFilter(FFmpegState ffmpegState, WatermarkState watermarkState, bool is10BitOutput)
    {
        _ffmpegState = ffmpegState;
        _watermarkState = watermarkState;
        _is10BitOutput = is10BitOutput;
    }

    public override string Filter
    {
        get
        {
            bool hasFadePoints = _watermarkState.MaybeFadePoints.Map(fp => fp.Count).IfNone(0) > 0;

            Option<string> maybeFormat = _ffmpegState.EncoderHardwareAccelerationMode switch
            {
                HardwareAccelerationMode.Nvenc when _is10BitOutput => "nv12",
                HardwareAccelerationMode.Nvenc => "yuva420p",
                HardwareAccelerationMode.Qsv => "yuva420p",
                _ when _watermarkState.Opacity != 100 || hasFadePoints =>
                    "yuva420p|yuva444p|yuva422p|rgba|abgr|bgra|gbrap|ya8",
                _ => None
            };

            return maybeFormat.Match(f => $"format={f}", () => string.Empty);
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
