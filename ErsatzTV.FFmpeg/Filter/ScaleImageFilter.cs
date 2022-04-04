namespace ErsatzTV.FFmpeg.Filter;

public class ScaleImageFilter : BaseFilter
{
    private readonly FrameSize _scaledSize;

    public ScaleImageFilter(FrameSize scaledSize)
    {
        _scaledSize = scaledSize;
    }

    public override string Filter => $"scale={_scaledSize.Width}:{_scaledSize.Height}";

    // public override IList<string> OutputOptions => new List<string> { "-q:v", "2" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        ScaledSize = _scaledSize,
        PaddedSize = _scaledSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
