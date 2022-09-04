using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class SubtitleScaleNppFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FrameSize _paddedSize;
    private readonly FrameSize _scaledSize;

    public SubtitleScaleNppFilter(FrameState currentState, FrameSize scaledSize, FrameSize paddedSize)
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
            if (_currentState.ScaledSize != _scaledSize)
            {
                string targetSize = $"{_paddedSize.Width}:{_paddedSize.Height}";
                string format = string.Empty;
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    format = $":format={pixelFormat.FFmpegName}";
                }

                scale = $"scale_npp={targetSize}{format}";
            }

            return scale;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
