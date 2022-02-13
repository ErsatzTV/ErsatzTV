namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class YadifCudaFilter : IPipelineFilterStep
{
    private readonly FrameState _currentState;

    public YadifCudaFilter(FrameState currentState)
    {
        _currentState = currentState;
    }

    public StreamKind StreamKind => StreamKind.Video;

    public string Filter =>
        _currentState.FrameDataLocation == FrameDataLocation.Hardware ? "yadif_cuda" : "hwupload_cuda,yadif_cuda";

    public FrameState NextState(FrameState currentState) => currentState with
    {
        Deinterlaced = true,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}