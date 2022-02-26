using ErsatzTV.FFmpeg.State;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.FFmpeg.Filter;

public class WatermarkPixelFormatFilter : BaseFilter
{
    private readonly FFmpegState _ffmpegState;
    private readonly WatermarkState _watermarkState;

    public WatermarkPixelFormatFilter(FFmpegState ffmpegState, WatermarkState watermarkState)
    {
        _ffmpegState = ffmpegState;
        _watermarkState = watermarkState;
    }

    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter
    {
        get
        {
            bool hasFadePoints = _watermarkState.MaybeFadePoints.Map(fp => fp.Count).IfNone(0) > 0;

            Option<string> maybeFormat = _ffmpegState.HardwareAccelerationMode switch
            {
                HardwareAccelerationMode.Nvenc => "yuva420p",
                HardwareAccelerationMode.Qsv => "yuva420p",
                _ when _watermarkState.Opacity != 100 || hasFadePoints =>
                    "yuva420p|yuva444p|yuva422p|rgba|abgr|bgra|gbrap|ya8",
                _ => None
            };

            return maybeFormat.Match(f => $"format={f}", () => string.Empty);
        }
    }
}
