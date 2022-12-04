using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class ColorspaceFilter : BaseFilter
{
    private readonly VideoStream _videoStream;
    private readonly IPixelFormat _desiredPixelFormat;

    public ColorspaceFilter(VideoStream videoStream, IPixelFormat desiredPixelFormat)
    {
        _videoStream = videoStream;
        _desiredPixelFormat = desiredPixelFormat;
    }

    public override FrameState NextState(FrameState currentState)
    {
        FrameState nextState = currentState with { FrameDataLocation = FrameDataLocation.Software };

        if (!_videoStream.ColorParams.IsUnknown && _desiredPixelFormat.BitDepth == 10 ||
            _desiredPixelFormat.BitDepth == 8)
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
            if (cp.IsMixed)
            {
                inputOverrides =
                    $"irange={cp.ColorRange}:ispace={cp.ColorSpace}:itrc={cp.ColorTransfer}:iprimaries={cp.ColorPrimaries}:";
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
