namespace ErsatzTV.FFmpeg.Option;

public class VideoTrackTimescaleOutputOption : IPipelineStep
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Unknown;
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> OutputOptions => new List<string> { "-video_track_timescale", "90000" };
    public FrameState NextState(FrameState currentState) => currentState;
}
