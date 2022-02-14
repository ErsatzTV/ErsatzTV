namespace ErsatzTV.FFmpeg.Option;

public class NoDemuxDecodeDelayOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-muxdelay", "0", "-muxpreload", "0" };
}
