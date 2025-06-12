using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class TonemapCudaFilter(IPixelFormat desiredPixelFormat) : BaseFilter
{
    public override string Filter =>
        $"libplacebo=tonemapping=auto:colorspace=bt709:color_primaries=bt709:color_trc=bt709:format={desiredPixelFormat.FFmpegName},hwupload_cuda";

    public override FrameState NextState(FrameState currentState) =>
        currentState with
        {
            PixelFormat = Some(desiredPixelFormat),
            FrameDataLocation = FrameDataLocation.Hardware
        };
}
