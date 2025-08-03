using System.Globalization;

namespace ErsatzTV.FFmpeg.OutputOption;

public class OutputTsOffsetOption(long ptsOffset, int videoTrackTimeScale) : OutputOption
{
    public override string[] OutputOptions =>
    [
        "-output_ts_offset",
        $"{(ptsOffset / (double)videoTrackTimeScale).ToString(NumberFormatInfo.InvariantInfo)}"
    ];

    // public override FrameState NextState(FrameState currentState) => currentState with { PtsOffset = _ptsOffset };
}
