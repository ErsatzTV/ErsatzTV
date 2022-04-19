namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class OverlaySubtitleQsvFilter : BaseFilter
{
    private readonly FrameState _currentState;

    public OverlaySubtitleQsvFilter(FrameState currentState)
    {
        _currentState = currentState;
    }

    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter =>
        $"overlay_qsv=eof_action=endall:shortest=1:repeatlast=0:w={_currentState.PaddedSize.Width}:h={_currentState.PaddedSize.Height}";
}
