using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class ScaleVaapiFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly bool _isAnamorphicEdgeCase;
    private readonly FrameSize _paddedSize;
    private readonly FrameSize _scaledSize;

    public ScaleVaapiFilter(
        FrameState currentState,
        FrameSize scaledSize,
        FrameSize paddedSize,
        bool isAnamorphicEdgeCase)
    {
        _currentState = currentState;
        _scaledSize = scaledSize;
        _paddedSize = paddedSize;
        _isAnamorphicEdgeCase = isAnamorphicEdgeCase;
    }

    public override string Filter
    {
        get
        {
            string scale = string.Empty;

            if (_currentState.ScaledSize == _scaledSize)
            {
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    // don't need scaling, but still need pixel format
                    scale = $"scale_vaapi=format={pixelFormat.FFmpegName}";
                }
            }
            else
            {
                string aspectRatio = string.Empty;
                if (_scaledSize != _paddedSize)
                {
                    aspectRatio = ":force_original_aspect_ratio=decrease";
                }

                string squareScale = string.Empty;
                var targetSize = $"{_paddedSize.Width}:{_paddedSize.Height}";
                string format = string.Empty;
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    format = $":format={pixelFormat.FFmpegName}";
                }

                if (_isAnamorphicEdgeCase)
                {
                    squareScale = $"scale_vaapi=iw:sar*ih{format},setsar=1,";
                }
                else if (_currentState.IsAnamorphic)
                {
                    squareScale = $"scale_vaapi=iw*sar:ih{format},setsar=1,";
                }
                else
                {
                    aspectRatio += ",setsar=1";
                }

                scale = $"{squareScale}scale_vaapi={targetSize}:force_divisible_by=2{format}{aspectRatio}";
            }

            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                return scale;
            }

            if (!string.IsNullOrWhiteSpace(scale))
            {
                return $"format=nv12|p010le|vaapi,hwupload,{scale}";
            }

            return string.Empty;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Hardware,
        IsAnamorphic = false // this filter always outputs square pixels
    };
}
