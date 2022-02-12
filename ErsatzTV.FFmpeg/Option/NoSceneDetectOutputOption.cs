namespace ErsatzTV.FFmpeg.Option;

public class NoSceneDetectOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-sc_threshold", "0" };
}
