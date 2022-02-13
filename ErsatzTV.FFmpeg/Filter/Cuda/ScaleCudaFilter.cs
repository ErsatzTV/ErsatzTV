namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class ScaleCudaFilter : IPipelineFilterStep
{
    private readonly FrameState _currentState;
    private readonly FrameSize _scaledSize;
    private readonly FrameSize _paddedSize;

    public ScaleCudaFilter(FrameState currentState, FrameSize scaledSize, FrameSize paddedSize)
    {
        _currentState = currentState;
        _scaledSize = scaledSize;
        _paddedSize = paddedSize;
    }

    public StreamKind StreamKind => StreamKind.Video;
    public string Filter
    {
        get
        {
            string scale;
            var format = $"format={_currentState.PixelFormat.FFmpegName}";

            if (_currentState.ScaledSize == _scaledSize)
            {
                // don't need scaling, but still need pixel format
                scale = $"scale_cuda={format}";
            }
            else
            {
                string targetSize = $"{_paddedSize.Width}:{_paddedSize.Height}";
                scale = $"scale_cuda={targetSize}:force_original_aspect_ratio=1:{format}";
            }

            return _currentState.FrameDataLocation == FrameDataLocation.Hardware ? scale : $"hwupload_cuda,{scale}";
        }
    }

    public FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
