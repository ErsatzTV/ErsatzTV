using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class ColorspaceFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly VideoStream _videoStream;
    private readonly IPixelFormat _desiredPixelFormat;
    private readonly bool _forceInputOverrides;
    private readonly FrameDataLocation _nextDataLocation;

    public ColorspaceFilter(
        FrameState currentState,
        VideoStream videoStream,
        IPixelFormat desiredPixelFormat,
        bool forceInputOverrides = false,
        FrameDataLocation nextDataLocation = FrameDataLocation.Software)
    {
        _currentState = currentState;
        _videoStream = videoStream;
        _desiredPixelFormat = desiredPixelFormat;
        _forceInputOverrides = forceInputOverrides;
        _nextDataLocation = nextDataLocation;
    }

    public override FrameState NextState(FrameState currentState)
    {
        FrameState nextState = currentState with { FrameDataLocation = _nextDataLocation };

        if (!_videoStream.ColorParams.IsUnknown && _desiredPixelFormat.BitDepth is 10 or 8)
        {
            nextState = nextState with { PixelFormat = Some(_desiredPixelFormat) };
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
                hwdownload = "hwdownload";
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

                inputOverrides =
                    $"irange={range}:ispace={cp.ColorSpace}:itrc={transfer}:iprimaries={primaries}:";
            }

            string colorspace = _desiredPixelFormat.BitDepth switch
            {
                _ when cp.IsUnknown => "setparams=range=tv:colorspace=bt709:color_trc=bt709:color_primaries=bt709",
                10 or 8 when !cp.IsUnknown =>
                    $"{hwdownload},colorspace={inputOverrides}all=bt709:format={_desiredPixelFormat.FFmpegName}",
                _ => string.Empty
            };

            return colorspace;
        }
    }
}
