using ErsatzTV.FFmpeg.Format;

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

            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    if (pixelFormat is PixelFormatVaapi)
                    {
                        foreach (IPixelFormat pf in AvailablePixelFormats.ForPixelFormat(pixelFormat.Name, null))
                        {
                            return $"hwdownload,format=vaapi|{pf.FFmpegName},{pad}";
                        }
                    }

                    return $"hwdownload,format={pixelFormat.FFmpegName},{pad}";
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
