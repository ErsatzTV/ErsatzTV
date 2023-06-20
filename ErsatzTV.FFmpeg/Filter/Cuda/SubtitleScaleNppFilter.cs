namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class SubtitleScaleNppFilter : BaseFilter
{
    private readonly FrameSize _paddedSize;

    public SubtitleScaleNppFilter(FrameSize paddedSize) => _paddedSize = paddedSize;

    public override string Filter =>
        $"scale_npp={_paddedSize.Width}:{_paddedSize.Height}:force_original_aspect_ratio=1";

    public override FrameState NextState(FrameState currentState) => currentState;
}
