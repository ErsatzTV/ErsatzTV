namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatMpegTs : IPipelineStep
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> OutputOptions => new List<string> { "-f", "mpegts" };
    public FrameState NextState(FrameState currentState) => currentState;
}
