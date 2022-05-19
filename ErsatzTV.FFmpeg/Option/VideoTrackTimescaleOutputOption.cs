namespace ErsatzTV.FFmpeg.Option;

public class VideoTrackTimescaleOutputOption : OutputOption
{
    private readonly int _timeScale;

    public VideoTrackTimescaleOutputOption(int timeScale) => _timeScale = timeScale;

    public override IList<string> OutputOptions => new List<string> { "-video_track_timescale", _timeScale.ToString() };

    public override FrameState NextState(FrameState currentState) =>
        currentState with { VideoTrackTimeScale = _timeScale };
}
