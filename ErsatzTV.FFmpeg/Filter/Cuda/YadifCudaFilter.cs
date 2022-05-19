namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class YadifCudaFilter : BaseFilter
{
    private readonly FrameState _currentState;

    public YadifCudaFilter(FrameState currentState) => _currentState = currentState;

    public override string Filter =>
        _currentState.FrameDataLocation == FrameDataLocation.Hardware ? "yadif_cuda" : "hwupload_cuda,yadif_cuda";

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        Deinterlaced = true,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
