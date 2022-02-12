namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatMpegTs : IPipelineStep
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => Array.Empty<string>();

    // always force an initial discontinuity
    public IList<string> OutputOptions =>
        new List<string> { "-f", "mpegts", "-mpegts_flags", "+initial_discontinuity" };

    public FrameState NextState(FrameState currentState) => currentState;
}
