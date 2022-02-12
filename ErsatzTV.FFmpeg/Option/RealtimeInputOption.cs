namespace ErsatzTV.FFmpeg.Option;

public class RealtimeInputOption : IPipelineStep
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Unknown;
    
    // some builds of ffmpeg seem to hang when realtime input is requested with multithreading,
    // so we force a single thread here
    public IList<string> GlobalOptions => new List<string> { "-threads", "1" };
    
    public IList<string> InputOptions => new List<string> { "-re" };
    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
}
