namespace ErsatzTV.FFmpeg.Filter;

public class HardwareUploadCudaFilter : BaseFilter
{
    private readonly FrameState _currentState;

    public HardwareUploadCudaFilter(FrameState currentState)
    {
        _currentState = currentState;
    }

    public override string Filter => _currentState.FrameDataLocation switch
    {
        FrameDataLocation.Hardware => string.Empty,
        _ => "hwupload_cuda"
    };

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Hardware };
}
