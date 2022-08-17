namespace ErsatzTV.FFmpeg.Filter;

public class SetDarFilter : BaseFilter
{
    private readonly string _displayAspectRatio;

    public SetDarFilter(string displayAspectRatio)
    {
        _displayAspectRatio = displayAspectRatio;
    }

    public override string Filter => $"setdar=dar={_displayAspectRatio.Replace(':', '/')}";
    public override FrameState NextState(FrameState currentState) => currentState;
}
