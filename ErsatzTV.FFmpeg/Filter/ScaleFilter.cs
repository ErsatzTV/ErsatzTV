using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class ScaleFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FrameSize _paddedSize;
    private readonly FrameSize _scaledSize;

    public ScaleFilter(FrameState currentState, FrameSize scaledSize, FrameSize paddedSize)
    {
        _currentState = currentState;
        _scaledSize = scaledSize;
        _paddedSize = paddedSize;
    }

    public override string Filter
    {
        get
        {
            string aspectRatio = string.Empty;
            if (_scaledSize != _paddedSize)
            {
                aspectRatio = ":force_original_aspect_ratio=decrease";
            }

            string scale =
                $"scale={_paddedSize.Width}:{_paddedSize.Height}:flags=fast_bilinear{aspectRatio}";

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

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
