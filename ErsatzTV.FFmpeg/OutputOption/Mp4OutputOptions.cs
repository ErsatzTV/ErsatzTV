namespace ErsatzTV.FFmpeg.OutputOption;

public class Mp4OutputOptions : OutputOption
{
    public override string[] OutputOptions =>
    [
        "-movflags", "+empty_moov+omit_tfhd_offset+frag_keyframe+default_base_moof"
    ];
}
