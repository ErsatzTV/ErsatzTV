using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class ScaleQsvFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FrameSize _scaledSize;

    public ScaleQsvFilter(FrameState currentState, FrameSize scaledSize)
    {
        _currentState = currentState;
        _scaledSize = scaledSize;
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
                    scale = $"scale_qsv=format={pixelFormat.FFmpegName}";
                }
            }
            else
            {
                string format = string.Empty;
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    format = $":format={pixelFormat.FFmpegName}";
                }

                string targetSize = $"{_scaledSize.Width}:{_scaledSize.Height}";
                scale = $"scale_qsv={targetSize}{format}";
            }

            // TODO: this might not always upload to hardware, so NextState could be inaccurate
            if (string.IsNullOrWhiteSpace(scale))
            {
                return scale;
            }

            return _currentState.FrameDataLocation == FrameDataLocation.Hardware
                ? scale
                : $"hwupload=extra_hw_frames=64,{scale}";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
