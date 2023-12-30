namespace ErsatzTV.FFmpeg.OutputOption;

public class NoDemuxDecodeDelayOutputOption : OutputOption
{
    public override string[] OutputOptions => new[] { "-muxdelay", "0", "-muxpreload", "0" };
}
