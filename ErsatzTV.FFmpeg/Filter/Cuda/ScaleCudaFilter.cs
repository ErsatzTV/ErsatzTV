using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class ScaleCudaFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FrameSize _paddedSize;
    private readonly FrameSize _scaledSize;

    public ScaleCudaFilter(FrameState currentState, FrameSize scaledSize, FrameSize paddedSize)
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
                    scale = $"scale_cuda=format={pixelFormat.FFmpegName}";
                }
            }
            else
            {
                string targetSize = $"{_paddedSize.Width}:{_paddedSize.Height}";
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

                scale = $"scale_cuda={targetSize}{aspectRatio}{format}";
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

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
