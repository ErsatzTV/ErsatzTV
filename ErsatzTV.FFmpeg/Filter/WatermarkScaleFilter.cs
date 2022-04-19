using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public class WatermarkScaleFilter : BaseFilter
{
    private readonly FrameSize _resolution;
    private readonly WatermarkState _watermarkState;

    public WatermarkScaleFilter(WatermarkState watermarkState, FrameSize resolution)
    {
        _watermarkState = watermarkState;
        _resolution = resolution;
    }

    public override string Filter
    {
        get
        {
            double width = Math.Round(_watermarkState.WidthPercent / 100.0 * _resolution.Width);
            return $"scale={width}:-1";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
