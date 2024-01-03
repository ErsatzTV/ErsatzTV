namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class VaapiSubtitlePixelFormatFilter : BaseFilter
{
    public override string Filter => "format=vaapi|yuva420p|yuva444p|yuva422p|rgba|abgr|bgra|gbrap|ya8";
    public override FrameState NextState(FrameState currentState) => currentState;
}
