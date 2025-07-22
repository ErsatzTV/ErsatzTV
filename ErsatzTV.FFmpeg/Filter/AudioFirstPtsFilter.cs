namespace ErsatzTV.FFmpeg.Filter;

public class AudioFirstPtsFilter(int pts) : BaseFilter
{
    public override string Filter => $"aresample=async=1:first_pts={pts}";

    public override FrameState NextState(FrameState currentState) => currentState;
}
