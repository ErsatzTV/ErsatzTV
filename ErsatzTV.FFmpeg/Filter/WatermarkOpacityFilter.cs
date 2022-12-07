using System.Globalization;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public class WatermarkOpacityFilter : BaseFilter
{
    private readonly WatermarkState _desiredState;

    public WatermarkOpacityFilter(WatermarkState desiredState) => _desiredState = desiredState;

    public override string Filter
    {
        get
        {
            double opacity = _desiredState.Opacity / 100.0;
            return $"colorchannelmixer=aa={opacity.ToString("F2", NumberFormatInfo.InvariantInfo)}";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
