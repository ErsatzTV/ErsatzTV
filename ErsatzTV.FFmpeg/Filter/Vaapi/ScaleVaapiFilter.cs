using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class ScaleVaapiFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FrameSize _scaledSize;
    private readonly FrameSize _paddedSize;

    public ScaleVaapiFilter(FrameState currentState, FrameSize scaledSize, FrameSize paddedSize)
    {
        _currentState = currentState;
        _scaledSize = scaledSize;
        _paddedSize = paddedSize;
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
                string format = string.Empty;
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    format = $":format={pixelFormat.FFmpegName}";
                }

                string aspectRatio = string.Empty;
                if (_scaledSize != _paddedSize)
                {
                    aspectRatio = ":force_original_aspect_ratio=1";
                }

                string targetSize = $"{_paddedSize.Width}:{_paddedSize.Height}";
                scale = $"scale_vaapi={targetSize}{aspectRatio}:force_divisible_by=2{format}";
            }

            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                return scale;
            }

            if (!string.IsNullOrWhiteSpace(scale))
            {
                return $"format=nv12|vaapi,hwupload,{scale}";
            }

            return "format=nv12|vaapi,hwupload";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
