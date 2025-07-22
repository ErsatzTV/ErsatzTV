using System.Globalization;

namespace ErsatzTV.FFmpeg.OutputOption;

public class OutputTsOffsetOption(double ptsOffset) : OutputOption
{
    public override string[] OutputOptions =>
    [
        "-output_ts_offset",
        $"{ptsOffset.ToString(NumberFormatInfo.InvariantInfo)}s"
    ];

    // public override FrameState NextState(FrameState currentState) => currentState with { PtsOffset = _ptsOffset };
}
