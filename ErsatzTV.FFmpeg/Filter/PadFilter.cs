namespace ErsatzTV.FFmpeg.Filter;

public class PadFilter : IPipelineFilterStep
{
    private readonly FrameSize _paddedSize;

    public PadFilter(FrameSize paddedSize)
    {
        _paddedSize = paddedSize;
    }

    public StreamKind StreamKind => StreamKind.Video;
    public string Filter => $"pad={_paddedSize.Width}:{_paddedSize.Height}:-1:-1:color=black";
    public FrameState NextState(FrameState currentState) => currentState with
    {
        PaddedSize = _paddedSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
