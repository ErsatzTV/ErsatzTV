namespace ErsatzTV.FFmpeg.Option;

public class Mp4OutputOptions : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-movflags", "+faststart+frag_keyframe+delay_moov" };
}
