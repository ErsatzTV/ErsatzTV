using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class ScaleCudaFilter : BaseFilter
{
    private readonly Option<FrameSize> _croppedSize;
    private readonly FrameState _currentState;
    private readonly bool _isAnamorphicEdgeCase;
    private readonly FrameSize _paddedSize;
    private readonly FrameSize _scaledSize;

    public ScaleCudaFilter(
        FrameState currentState,
        FrameSize scaledSize,
        FrameSize paddedSize,
        Option<FrameSize> croppedSize,
        bool isAnamorphicEdgeCase)
    {
        _currentState = currentState;
        _scaledSize = scaledSize;
        _paddedSize = paddedSize;
        _croppedSize = croppedSize;
        _isAnamorphicEdgeCase = isAnamorphicEdgeCase;
    }

    public bool IsFormatOnly => _currentState.ScaledSize == _scaledSize;

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
                    scale = $"scale_cuda=format={pixelFormat.FFmpegName}";
                }
            }
            else
            {
                string aspectRatio = string.Empty;
                if (_scaledSize != _paddedSize)
                {
                    aspectRatio = _croppedSize.IsSome
                        ? ":force_original_aspect_ratio=increase"
                        : ":force_original_aspect_ratio=decrease";
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
                    squareScale = $"scale_cuda=iw:sar*ih{format},setsar=1,";
                }
                else if (_currentState.IsAnamorphic)
                {
                    squareScale = $"scale_cuda=iw*sar:ih{format},setsar=1,";
                }
                else
                {
                    aspectRatio += ",setsar=1";
                }

                scale = $"{squareScale}scale_cuda={targetSize}{format}{aspectRatio}";
            }

            // TODO: this might not always upload to hardware, so NextState could be inaccurate
            if (string.IsNullOrWhiteSpace(scale))
            {
                return scale;
            }

            return _currentState.FrameDataLocation == FrameDataLocation.Hardware
                ? scale
                : $"hwupload_cuda,{scale}";
        }
    }

    public override FrameState NextState(FrameState currentState)
    {
        FrameState result = currentState with
        {
            ScaledSize = _scaledSize,
            PaddedSize = _scaledSize,
            FrameDataLocation = FrameDataLocation.Hardware,
            IsAnamorphic = false // this filter always outputs square pixels
        };

        foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
        {
            result = result with { PixelFormat = Some(pixelFormat) };
        }

        return result;
    }
}
