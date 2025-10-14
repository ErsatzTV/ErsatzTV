namespace ErsatzTV.FFmpeg.Filter;

public class ScaleSubtitleImageFilter(FrameSize scaledSize) : BaseFilter
{
    public override string Filter =>
        $"scale={scaledSize.Width}:{scaledSize.Height}:force_original_aspect_ratio=decrease,pad=w={scaledSize.Width}:h={scaledSize.Height}:x=-1:y=-1:color=black@0";

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = scaledSize,
        PaddedSize = scaledSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
