namespace ErsatzTV.FFmpeg.Filter;

public class YadifFilter : IPipelineFilterStep
{
    public StreamKind StreamKind => StreamKind.Video;
    public string Filter => "yadif=1";
    public FrameState NextState(FrameState currentState) => currentState with
    {
        Deinterlaced = true,
        FrameDataLocation = FrameDataLocation.Software
    };
}