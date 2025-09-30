namespace ErsatzTV.FFmpeg.OutputOption;

public class HlsDirectMp4OutputOptions : OutputOption
{
    public override string[] OutputOptions =>
    [
        "-movflags", "+faststart+frag_keyframe+separate_moof+omit_tfhd_offset+empty_moov+delay_moov"
    ];
}
