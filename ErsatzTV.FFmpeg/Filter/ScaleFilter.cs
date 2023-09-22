using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class ScaleFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly bool _isAnamorphicEdgeCase;
    private readonly FrameSize _paddedSize;
    private readonly Option<FrameSize> _croppedSize;
    private readonly FrameSize _scaledSize;

    public ScaleFilter(
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

    public override string Filter
    {
        get
        {
            if (_currentState.ScaledSize == _scaledSize)
            {
                return string.Empty;
            }

            string aspectRatio = string.Empty;
            if (_scaledSize != _paddedSize)
            {
                aspectRatio = _croppedSize.IsSome
                    ? ":force_original_aspect_ratio=increase"
                    : ":force_original_aspect_ratio=decrease";
            }

            string scale;
            if (_isAnamorphicEdgeCase)
            {
                scale =
                    $"scale=iw:sar*ih,setsar=1,scale={_paddedSize.Width}:{_paddedSize.Height}:flags=fast_bilinear{aspectRatio}";
            }
            else if (_currentState.IsAnamorphic)
            {
                scale =
                    $"scale=iw*sar:ih,setsar=1,scale={_paddedSize.Width}:{_paddedSize.Height}:flags=fast_bilinear{aspectRatio}";
            }
            else
            {
                scale = $"scale={_paddedSize.Width}:{_paddedSize.Height}:flags=fast_bilinear{aspectRatio},setsar=1";
            }

            string hwdownload = string.Empty;
            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                hwdownload = "hwdownload,";
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    if (pixelFormat.FFmpegName == FFmpegFormat.NV12)
                    {
                        hwdownload = "hwdownload,format=nv12,";
                    }
                }
            }

            return $"{hwdownload}{scale}";
        }
    }

    public override FrameState NextState(FrameState currentState) =>
        currentState with
        {
            ScaledSize = _scaledSize,
            PaddedSize = _scaledSize,
            FrameDataLocation = FrameDataLocation.Software,
            IsAnamorphic = false // this filter always outputs square pixels
        };
}
