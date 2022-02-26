using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public class WatermarkScaleFilter : BaseFilter
{
    private readonly WatermarkState _watermarkState;
    private readonly FrameSize _resolution;

    public WatermarkScaleFilter(WatermarkState watermarkState, FrameSize resolution)
    {
        _watermarkState = watermarkState;
        _resolution = resolution;
    }

    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter
    {
        get
        {
            double width = Math.Round(_watermarkState.WidthPercent / 100.0 * _resolution.Width);
            return $"scale={width}:-1";
        }
    }
}
