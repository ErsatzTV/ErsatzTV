namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class CudaSoftwareFallbackUploadFilter : BaseFilter
{
    public override string Filter => "hwupload";

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Hardware };
}
