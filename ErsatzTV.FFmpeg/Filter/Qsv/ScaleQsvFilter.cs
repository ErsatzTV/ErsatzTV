using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class ScaleQsvFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FrameSize _scaledSize;
    private readonly int _extraHardwareFrames;
    private readonly bool _isAnamorphicEdgeCase;
    private readonly string _sampleAspectRatio;

    public ScaleQsvFilter(
        FrameState currentState,
        FrameSize scaledSize,
        int extraHardwareFrames,
        bool isAnamorphicEdgeCase,
        string sampleAspectRatio)
    {
        _currentState = currentState;
        _scaledSize = scaledSize;
        _extraHardwareFrames = extraHardwareFrames;
        _isAnamorphicEdgeCase = isAnamorphicEdgeCase;
        _sampleAspectRatio = sampleAspectRatio;
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
                string squareScale = string.Empty;
                string targetSize = $"w={_scaledSize.Width}:h={_scaledSize.Height}";
                string format = string.Empty;
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    format = $":format={pixelFormat.FFmpegName}";
                }

                string sar = _sampleAspectRatio.Replace(':', '/');
                if (_isAnamorphicEdgeCase)
                {
                    squareScale = $"vpp_qsv=w=iw:h={sar}*ih{format},setsar=1,";
                }
                else if (_currentState.IsAnamorphic)
                {
                    squareScale = $"vpp_qsv=w=iw*{sar}:h=ih{format},setsar=1,";
                }
                else
                {
                    format += ",setsar=1";
                }

                scale = $"{squareScale}vpp_qsv={targetSize}{format}";
            }

            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                return scale;
            }

            string initialPixelFormat = _currentState.PixelFormat.Match(pf => pf.FFmpegName, FFmpegFormat.NV12);
            if (!string.IsNullOrWhiteSpace(scale))
            {
                return $"format={initialPixelFormat},hwupload=extra_hw_frames={_extraHardwareFrames},{scale}";
            }

            return string.Empty;
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

        if (_currentState.PixelFormat.IsNone &&
            _currentState.FrameDataLocation == FrameDataLocation.Software &&
            currentState.PixelFormat.Map(pf => pf is not PixelFormatNv12).IfNone(false))
        {
            // wrap in nv12
            result = result with
            {
                PixelFormat = currentState.PixelFormat
                    .Map(pf => (IPixelFormat)new PixelFormatNv12(pf.Name))
            };
        }
        else
        {
            foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
            {
                result = result with { PixelFormat = Some(pixelFormat) };
            }
        }

        return result;
    }
}
