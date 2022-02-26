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
            // use vpp_qsv because scale_qsv sometimes causes green lines at the bottom 

            string scale = string.Empty;

            if (_currentState.ScaledSize == _scaledSize)
            {
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    // don't need scaling, but still need pixel format
                    scale = $"vpp_qsv=format={pixelFormat.FFmpegName}";
                }
            }
            else
            {
                string format = string.Empty;
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    format = $":format={pixelFormat.FFmpegName}";
                }

                string targetSize = $"w={_scaledSize.Width}:h={_scaledSize.Height}";
                scale = $"vpp_qsv={targetSize}{format}";
            }

            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                return scale;
            }

            string initialPixelFormat = _currentState.PixelFormat.Match(pf => pf.FFmpegName, FFmpegFormat.NV12);
            if (!string.IsNullOrWhiteSpace(scale))
            {
                return $"format={initialPixelFormat},hwupload=extra_hw_frames=64,{scale}";
            }

            return $"format={initialPixelFormat},hwupload=extra_hw_frames=64";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
