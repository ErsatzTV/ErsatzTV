namespace ErsatzTV.FFmpeg.Option;

public class Mp4OutputOptions : OutputOption
{
    public override IList<string> OutputOptions => new List<string>
        { "-movflags", "+faststart+frag_keyframe+separate_moof+omit_tfhd_offset+empty_moov+delay_moov" };
}
