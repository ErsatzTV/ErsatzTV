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

    public override FrameState NextState(FrameState currentState) =>
        currentState with
        {
            PixelFormat = Some(_desiredPixelFormat),
            FrameDataLocation = FrameDataLocation.Software
        };

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
                10 when !cp.IsUnknown => $"colorspace={inputOverrides}all=bt709:format=yuv420p10",
                8 when !cp.IsUnknown => $"colorspace={inputOverrides}all=bt709:format=yuv420p",
                _ => string.Empty
            };

            return colorspace;
        }
    }
}
