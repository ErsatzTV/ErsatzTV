namespace ErsatzTV.FFmpeg.Option;

public class VideoTrackTimescaleOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-video_track_timescale", "90000" };
}
