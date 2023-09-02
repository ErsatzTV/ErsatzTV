namespace ErsatzTV.FFmpeg.OutputOption;

public class NoDemuxDecodeDelayOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-muxdelay", "0", "-muxpreload", "0" };
}
