using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class ColorspaceFilter : BaseFilter
{
    private readonly VideoStream _videoStream;
    private readonly IPixelFormat _desiredPixelFormat;
    private readonly bool _forceInputOverrides;
    private readonly FrameDataLocation _nextDataLocation;

    public ColorspaceFilter(
        VideoStream videoStream,
        IPixelFormat desiredPixelFormat,
        bool forceInputOverrides = false,
        FrameDataLocation nextDataLocation = FrameDataLocation.Software)
    {
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
            string inputOverrides = string.Empty;
            ColorParams cp = _videoStream.ColorParams;
            if (cp.IsMixed || _forceInputOverrides)
            {
                string range = string.IsNullOrWhiteSpace(cp.ColorRange) ? "tv" : cp.ColorRange;

                inputOverrides =
                    $"irange={range}:ispace={cp.ColorSpace}:itrc={cp.ColorTransfer}:iprimaries={cp.ColorPrimaries}:";
            }

            string colorspace = _desiredPixelFormat.BitDepth switch
            {
                _ when cp.IsUnknown => "setparams=range=tv:colorspace=bt709:color_trc=bt709:color_primaries=bt709",
                10 or 8 when !cp.IsUnknown =>
                    $"colorspace={inputOverrides}all=bt709:format={_desiredPixelFormat.FFmpegName}",
                _ => string.Empty
            };

            return colorspace;
        }
    }
}
