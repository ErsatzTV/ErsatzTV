using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class ColorspaceFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly VideoStream _videoStream;
    private readonly IPixelFormat _desiredPixelFormat;
    private readonly bool _forceInputOverrides;

    public ColorspaceFilter(
        FrameState currentState,
        VideoStream videoStream,
        IPixelFormat desiredPixelFormat,
        bool forceInputOverrides = false)
    {
        _currentState = currentState;
        _videoStream = videoStream;
        _desiredPixelFormat = desiredPixelFormat;
        _forceInputOverrides = forceInputOverrides;
    }

    public override FrameState NextState(FrameState currentState)
    {
        FrameState nextState = currentState;

        if (!_videoStream.ColorParams.IsUnknown && _desiredPixelFormat.BitDepth is 10 or 8)
        {
            nextState = nextState with
            {
                FrameDataLocation = FrameDataLocation.Software,
                PixelFormat = Some(_desiredPixelFormat)
            };
        }

        return nextState;
    }

    public override string Filter
    {
        get
        {
            string hwdownload = string.Empty;
            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                hwdownload = "hwdownload,";
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    if (!string.IsNullOrWhiteSpace(pixelFormat.FFmpegName))
                    {
                        hwdownload = $"hwdownload,format={pixelFormat.FFmpegName},";
                    }
                }
            }

            string inputOverrides = string.Empty;
            ColorParams cp = _videoStream.ColorParams;
            if (cp.IsMixed || _forceInputOverrides)
            {
                string range = string.IsNullOrWhiteSpace(cp.ColorRange) ? "tv" : cp.ColorRange;
                string transfer = string.IsNullOrWhiteSpace(cp.ColorTransfer)
                    ? "bt709"
                    : cp.ColorTransfer;
                string primaries = string.IsNullOrWhiteSpace(cp.ColorPrimaries)
                    ? "bt709"
                    : cp.ColorPrimaries;
                string space = string.IsNullOrWhiteSpace(cp.ColorSpace)
                    ? "bt709"
                    : cp.ColorSpace;

                inputOverrides =
                    $"irange={range}:ispace={space}:itrc={transfer}:iprimaries={primaries}:";
            }

            string colorspace = _desiredPixelFormat.BitDepth switch
            {
                _ when cp.IsUnknown => "setparams=range=tv:colorspace=bt709:color_trc=bt709:color_primaries=bt709",
                10 when !cp.IsUnknown =>
                    $"{hwdownload}colorspace={inputOverrides}all=bt709:format=yuv420p10",
                8 when !cp.IsUnknown =>
                    $"{hwdownload}colorspace={inputOverrides}all=bt709:format=yuv420p",
                _ => string.Empty
            };

            return colorspace;
        }
    }
}
