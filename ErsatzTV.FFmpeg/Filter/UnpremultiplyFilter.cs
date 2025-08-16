namespace ErsatzTV.FFmpeg.Filter;

public class UnpremultiplyFilter : BaseFilter
{
    public override string Filter => "unpremultiply=inplace=1";

    public override FrameState NextState(FrameState currentState) => currentState;
}
