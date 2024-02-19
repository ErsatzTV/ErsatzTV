namespace ErsatzTV.FFmpeg.Filter;

public class AudioFirstPtsFilter : BaseFilter
{
    private readonly int _pts;

    public AudioFirstPtsFilter(int pts) => _pts = pts;

    public override string Filter => $"aresample=async=1:first_pts={_pts}";

    public override FrameState NextState(FrameState currentState) => currentState;
}
