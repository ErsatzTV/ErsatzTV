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
            string scale;
            var format = $"format={_currentState.PixelFormat.FFmpegName}";

            if (_currentState.ScaledSize == _scaledSize)
            {
                // don't need scaling, but still need pixel format
                scale = $"scale_vaapi={format}";
            }
            else
            {
                string targetSize = $"{_paddedSize.Width}:{_paddedSize.Height}";
                scale = $"scale_vaapi={targetSize}:force_original_aspect_ratio=1:force_divisible_by=2:{format}";
            }

            return _currentState.FrameDataLocation == FrameDataLocation.Hardware ? scale : $"hwupload,{scale}";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
