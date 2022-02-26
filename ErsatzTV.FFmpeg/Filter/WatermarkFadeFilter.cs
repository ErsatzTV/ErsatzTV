using System.Globalization;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public class WatermarkFadeFilter : BaseFilter
{
    private readonly WatermarkFadePoint _fadePoint;

    public WatermarkFadeFilter(WatermarkFadePoint fadePoint)
    {
        _fadePoint = fadePoint;
    }

    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter
    {
        get
        {
            var startTime = _fadePoint.Time.TotalSeconds.ToString(NumberFormatInfo.InvariantInfo);
            var enableStart = _fadePoint.EnableStart.TotalSeconds.ToString(NumberFormatInfo.InvariantInfo);
            var enableFinish = _fadePoint.EnableFinish.TotalSeconds.ToString(NumberFormatInfo.InvariantInfo);

            string inOut = _fadePoint switch
            {
                WatermarkFadeIn => "in",
                _ => "out"
            };

            return $"fade={inOut}:st={startTime}:d=1:alpha=1:enable='between(t,{enableStart},{enableFinish})'";
        }
    }
}
