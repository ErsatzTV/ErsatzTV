namespace ErsatzTV.FFmpeg.Filter;

public class ScaleFilter : IPipelineFilterStep
{
    private readonly FrameSize _scaledSize;

    public ScaleFilter(FrameSize scaledSize)
    {
        _scaledSize = scaledSize;
    }

    public StreamKind StreamKind => StreamKind.Video;
    public string Filter => $"scale={_scaledSize.Width}:{_scaledSize.Height}:flags=fast_bilinear";
    public FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
