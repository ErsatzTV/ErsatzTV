using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class OverlaySubtitleFilter : BaseFilter
{
    private readonly IPixelFormat _outputPixelFormat;

    public OverlaySubtitleFilter(IPixelFormat outputPixelFormat) => _outputPixelFormat = outputPixelFormat;

    public override string Filter =>
        $"overlay=x=(W-w)/2:y=(H-h)/2:format={(_outputPixelFormat.BitDepth == 10 ? '1' : '0')}";

    public override FrameState NextState(FrameState currentState) => currentState;
}
