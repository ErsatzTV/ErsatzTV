namespace ErsatzTV.FFmpeg.Filter;

public class PadFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FrameSize _paddedSize;

    public PadFilter(FrameState currentState, FrameSize paddedSize)
    {
        _currentState = currentState;
        _paddedSize = paddedSize;
    }

    public override string Filter
    {
        get
        {
            string pad = $"pad={_paddedSize.Width}:{_paddedSize.Height}:-1:-1:color=black";
            string pixelFormat = _currentState.PixelFormat.Match(pf => pf.FFmpegName, () => string.Empty);

            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                if (!string.IsNullOrWhiteSpace(pixelFormat))
                {
                    return $"hwdownload,format={pixelFormat},{pad}";
                }

                return $"hwdownload,{pad}";
            }

            return pad;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        PaddedSize = _paddedSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
