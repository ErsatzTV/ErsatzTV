namespace ErsatzTV.FFmpeg.Filter;

public class FrameRateFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly int _frameRate;

    public FrameRateFilter(FrameState currentState, int frameRate)
    {
        _currentState = currentState;
        _frameRate = frameRate;
    }

    public override string Filter
    {
        get
        {
            string frameRate = $"framerate=fps={_frameRate}:flags=-scd";
            string pixelFormat = _currentState.PixelFormat.Match(pf => pf.FFmpegName, () => string.Empty);

            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                if (!string.IsNullOrWhiteSpace(pixelFormat))
                {
                    return $"hwdownload,format={pixelFormat},{frameRate}";
                }

                return $"hwdownload,{frameRate}";
            }

            return frameRate;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with { FrameRate = _frameRate };
}
