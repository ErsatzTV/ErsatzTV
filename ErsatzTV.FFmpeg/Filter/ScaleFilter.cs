using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class ScaleFilter : IPipelineFilterStep
{
    private readonly FrameState _currentState;
    private readonly FrameSize _scaledSize;
    private readonly FrameSize _paddedSize;

    public ScaleFilter(FrameState currentState, FrameSize scaledSize, FrameSize paddedSize)
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
            string scale =
                $"scale={_paddedSize.Width}:{_paddedSize.Height}:flags=fast_bilinear:force_original_aspect_ratio=decrease";

            string hwdownload = string.Empty;
            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                switch (_currentState.PixelFormat.FFmpegName)
                {
                    case FFmpegFormat.NV12:
                        hwdownload = "hwdownload,format=nv12,";
                        break;
                    default:
                        hwdownload = "hwdownload,";
                        break;
                }
            }

            return $"{hwdownload}{scale}";
        }
    }

    public FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
