using System.Globalization;

namespace ErsatzTV.FFmpeg.OutputOption;

public class OutputTsOffsetOption(TimeSpan ptsOffset) : OutputOption
{
    public override string[] OutputOptions =>
    [
        "-output_ts_offset",
        $"{ptsOffset.TotalSeconds.ToString(NumberFormatInfo.InvariantInfo)}s"
    ];

    // public override FrameState NextState(FrameState currentState) => currentState with { PtsOffset = _ptsOffset };
}
