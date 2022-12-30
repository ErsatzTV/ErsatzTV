using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class TonemapFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly IPixelFormat _desiredPixelFormat;

    public TonemapFilter(FrameState currentState, IPixelFormat desiredPixelFormat)
    {
        _currentState = currentState;
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
            string pixelFormat = _currentState.PixelFormat.Match(pf => pf.FFmpegName, () => string.Empty);

            var tonemap =
                $"setparams=colorspace=bt2020c,zscale=transfer=linear,tonemap=hable,zscale=transfer=bt709,format={_desiredPixelFormat.FFmpegName}";
            
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
}
