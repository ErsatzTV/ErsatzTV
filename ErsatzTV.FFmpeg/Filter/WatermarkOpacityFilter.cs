using System.Globalization;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public class WatermarkOpacityFilter : BaseFilter
{
    private readonly WatermarkState _desiredState;
    private readonly bool _is10BitOutput;

    public WatermarkOpacityFilter(WatermarkState desiredState, bool is10BitOutput)
    {
        _desiredState = desiredState;
        _is10BitOutput = is10BitOutput;
    }

    public override string Filter
    {
        get
        {
            string pixelFormat = _is10BitOutput ? ",format=nv12" : string.Empty;

            double opacity = _desiredState.Opacity / 100.0;
            return $"colorchannelmixer=aa={opacity.ToString("F2", NumberFormatInfo.InvariantInfo)}{pixelFormat}";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
