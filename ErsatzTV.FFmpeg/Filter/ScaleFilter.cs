namespace ErsatzTV.FFmpeg.Filter;

public class ScaleFilter : IPipelineFilterStep
{
    private readonly FrameSize _scaledSize;
    private readonly FrameSize _paddedSize;

    public ScaleFilter(FrameSize scaledSize, FrameSize paddedSize)
    {
        _scaledSize = scaledSize;
        _paddedSize = paddedSize;
    }

    public StreamKind StreamKind => StreamKind.Video;
    public string Filter => $"scale={_paddedSize.Width}:{_paddedSize.Height}:flags=fast_bilinear:force_original_aspect_ratio=decrease";
    public FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
