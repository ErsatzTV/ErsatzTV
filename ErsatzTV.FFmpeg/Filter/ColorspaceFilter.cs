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
            string setParams = string.Empty;
            if (_videoStream.ColorParams.IsUnknown)
            {
                setParams = "setparams=range=tv:colorspace=bt709:color_trc=bt709:color_primaries=bt709";
            }

            string colorspace = _desiredPixelFormat.BitDepth switch
            {
                10 when !_videoStream.ColorParams.IsUnknown => "colorspace=all=bt709:format=yuv420p10",
                8 when !_videoStream.ColorParams.IsUnknown => "colorspace=all=bt709:format=yuv420p",
                _ => string.Empty
            };

            return string.Join(',', new[] { setParams, colorspace }.Filter(s => !string.IsNullOrWhiteSpace(s)));
        }
    }
}
