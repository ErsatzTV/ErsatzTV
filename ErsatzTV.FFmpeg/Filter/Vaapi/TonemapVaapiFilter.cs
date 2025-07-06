namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class TonemapVaapiFilter(FFmpegState ffmpegState) : BaseFilter
{
    public override string Filter =>
        $"hwupload=derive_device=vaapi,hwmap=derive_device=opencl,tonemap_opencl=tonemap={ffmpegState.TonemapAlgorithm},hwmap=derive_device=vaapi:reverse=1";

    public override FrameState NextState(FrameState currentState) =>
        currentState with
        {
            FrameDataLocation = FrameDataLocation.Hardware
        };
}
