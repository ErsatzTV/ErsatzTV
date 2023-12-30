using System.Globalization;

namespace ErsatzTV.FFmpeg.OutputOption;

public class VideoTrackTimescaleOutputOption : OutputOption
{
    private readonly int _timeScale;

    public VideoTrackTimescaleOutputOption(int timeScale) => _timeScale = timeScale;

    public override string[] OutputOptions => new[]
        { "-video_track_timescale", _timeScale.ToString(CultureInfo.InvariantCulture) };

    public override FrameState NextState(FrameState currentState) =>
        currentState with { VideoTrackTimeScale = _timeScale };
}
