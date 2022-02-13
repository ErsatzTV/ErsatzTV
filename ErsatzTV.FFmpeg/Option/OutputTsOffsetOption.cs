using System.Globalization;

namespace ErsatzTV.FFmpeg.Option;

public class OutputTsOffsetOption : OutputOption
{
    private readonly long _ptsOffset;
    private readonly int _videoTrackTimeScale;

    public OutputTsOffsetOption(long ptsOffset, int videoTrackTimeScale)
    {
        _ptsOffset = ptsOffset;
        _videoTrackTimeScale = videoTrackTimeScale;
    }

    public override IList<string> OutputOptions => new List<string>
    {
        "-output_ts_offset",
        $"{(_ptsOffset / (double)_videoTrackTimeScale).ToString(NumberFormatInfo.InvariantInfo)}"
    };

    public override FrameState NextState(FrameState currentState) => currentState with { PtsOffset = _ptsOffset };
}
