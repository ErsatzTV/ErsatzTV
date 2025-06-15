using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class TonemapFilter : BaseFilter
{
    private readonly FFmpegState _ffmpegState;
    private readonly FrameState _currentState;
    private readonly IPixelFormat _desiredPixelFormat;

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
            string pixelFormat = _currentState.PixelFormat.Match(pf => pf.FFmpegName, () => string.Empty);

            var tonemap =
                $"zscale=transfer=linear,tonemap={_ffmpegState.TonemapAlgorithm},zscale=transfer=bt709,format={_desiredPixelFormat.FFmpegName}";

            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                if (!string.IsNullOrWhiteSpace(pixelFormat))
                {
                    return $"hwdownload,format={pixelFormat},{tonemap}";
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
