namespace ErsatzTV.FFmpeg.Protocol;

public class PipeProtocol : IPipelineStep
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> OutputOptions => new List<string> { "pipe:1" };
    public FrameState NextState(FrameState currentState) => currentState;
}
