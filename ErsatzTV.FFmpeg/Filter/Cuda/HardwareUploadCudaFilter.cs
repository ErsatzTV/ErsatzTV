namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class HardwareUploadCudaFilter(FrameDataLocation frameDataLocation) : BaseFilter
{
    public override string Filter => frameDataLocation switch
    {
        FrameDataLocation.Hardware => string.Empty,
        _ => "hwupload_cuda"
    };

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Hardware };
}
