using System.Globalization;

namespace ErsatzTV.FFmpeg.OutputOption;

public class OutputTsOffsetOption : OutputOption
{
    private readonly long _ptsOffset;
    private readonly int _videoTrackTimeScale;

    public OutputTsOffsetOption(long ptsOffset, int videoTrackTimeScale)
    {
        _ptsOffset = ptsOffset;
        _videoTrackTimeScale = videoTrackTimeScale;
    }

    public override string[] OutputOptions => new[]
    {
        "-output_ts_offset",
        $"{(_ptsOffset / (double)_videoTrackTimeScale).ToString(NumberFormatInfo.InvariantInfo)}"
    };

    // public override FrameState NextState(FrameState currentState) => currentState with { PtsOffset = _ptsOffset };
}
