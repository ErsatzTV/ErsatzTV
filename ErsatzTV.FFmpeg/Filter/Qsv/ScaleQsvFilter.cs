namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class ScaleQsvFilter : IPipelineFilterStep
{
    private readonly FrameState _currentState;
    private readonly FrameSize _scaledSize;

    public ScaleQsvFilter(FrameState currentState, FrameSize scaledSize)
    {
        _currentState = currentState;
        _scaledSize = scaledSize;
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
                scale = $"scale_qsv={format}";
            }
            else
            {
                string targetSize = $"{_scaledSize.Width}:{_scaledSize.Height}";
                scale = $"scale_qsv={targetSize}:{format}";
            }

            return _currentState.FrameDataLocation == FrameDataLocation.Hardware
                ? scale
                : $"hwupload=extra_hw_frames=64,{scale}";
        }
    }

    public FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
