namespace ErsatzTV.FFmpeg.Filter;

public class PadFilter : IPipelineFilterStep
{
    private readonly FrameState _currentState;
    private readonly FrameSize _paddedSize;

    public PadFilter(FrameState currentState, FrameSize paddedSize)
    {
        _currentState = currentState;
        _paddedSize = paddedSize;
    }

    public StreamKind StreamKind => StreamKind.Video;
    public string Filter
    {
        get
        {
            string pad = $"pad={_paddedSize.Width}:{_paddedSize.Height}:-1:-1:color=black";
            return _currentState.FrameDataLocation == FrameDataLocation.Hardware
                ? $"hwdownload,format={_currentState.PixelFormat.Name},{pad}" // TODO: does this apply to other accels?
                : pad;
        }
    }

    public FrameState NextState(FrameState currentState) => currentState with
    {
        PaddedSize = _paddedSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
