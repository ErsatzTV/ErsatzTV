using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class TonemapVaapiFilter(FFmpegState ffmpegState) : BaseFilter
{
    public override string Filter =>
        $"hwupload=derive_device=vaapi,hwmap=derive_device=opencl,tonemap_opencl=tonemap={ffmpegState.TonemapAlgorithm},hwmap=derive_device=vaapi:reverse=1,scale_vaapi=format=p010le";

    public override FrameState NextState(FrameState currentState) =>
        currentState with
        {
            FrameDataLocation = FrameDataLocation.Hardware,
            PixelFormat = new PixelFormatVaapi(PixelFormat.YUV420P10LE)
        };
}
