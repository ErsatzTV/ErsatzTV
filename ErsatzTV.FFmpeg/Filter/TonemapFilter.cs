using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class TonemapFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly IPixelFormat _desiredPixelFormat;
    private readonly FFmpegState _ffmpegState;

    public TonemapFilter(FFmpegState ffmpegState, FrameState currentState, IPixelFormat desiredPixelFormat)
    {
        _ffmpegState = ffmpegState;
        _currentState = currentState;
        _desiredPixelFormat = desiredPixelFormat;
    }

    public override string Filter
    {
        get
        {
            var tonemap =
                $"zscale=transfer=linear,tonemap={_ffmpegState.TonemapAlgorithm},zscale=transfer=bt709,format={_desiredPixelFormat.FFmpegName}";

            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    if (pixelFormat is PixelFormatCuda)
                    {
                        foreach (IPixelFormat pf in AvailablePixelFormats.ForPixelFormat(pixelFormat.Name, null))
                        {
                            return $"hwdownload,format={pf.FFmpegName},{tonemap}";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(pixelFormat.FFmpegName))
                    {
                        return $"hwdownload,format={pixelFormat.FFmpegName},{tonemap}";
                    }
                }

                return $"hwdownload,{tonemap}";
            }

            return tonemap;
        }
    }

    public override FrameState NextState(FrameState currentState) =>
        currentState with
        {
            PixelFormat = Some(_desiredPixelFormat),
            FrameDataLocation = FrameDataLocation.Software
        };
}
