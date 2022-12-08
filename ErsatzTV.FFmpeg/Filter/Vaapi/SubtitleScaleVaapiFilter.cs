namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class SubtitleScaleVaapiFilter : BaseFilter
{
    private readonly FrameSize _paddedSize;

    public SubtitleScaleVaapiFilter(FrameSize paddedSize)
    {
        _paddedSize = paddedSize;
    }

    public override string Filter =>
        $"scale_vaapi={_paddedSize.Width}:{_paddedSize.Height}:force_original_aspect_ratio=decrease";

    public override FrameState NextState(FrameState currentState) => currentState;
}
