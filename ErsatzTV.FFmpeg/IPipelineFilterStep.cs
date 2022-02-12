namespace ErsatzTV.FFmpeg;

public interface IPipelineFilterStep
{
    StreamKind StreamKind { get; }
    string Filter { get; }
    FrameState NextState(FrameState currentState);
}
