namespace ErsatzTV.FFmpeg.Filter;

public class ScaleImageFilter : BaseFilter
{
    private readonly FrameSize _scaledSize;

    public ScaleImageFilter(FrameSize scaledSize) => _scaledSize = scaledSize;

    public override string Filter =>
        $"scale={_scaledSize.Width}:{_scaledSize.Height}:force_original_aspect_ratio=decrease";

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
