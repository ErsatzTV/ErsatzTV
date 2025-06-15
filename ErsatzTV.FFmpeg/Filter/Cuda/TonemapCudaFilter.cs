using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class TonemapCudaFilter(FFmpegState ffmpegState, IPixelFormat desiredPixelFormat) : BaseFilter
{
    public override string Filter =>
        $"libplacebo=tonemapping={ffmpegState.TonemapAlgorithm}:colorspace=bt709:color_primaries=bt709:color_trc=bt709:format={desiredPixelFormat.FFmpegName},hwupload_cuda";

    public override FrameState NextState(FrameState currentState) =>
        currentState with
        {
            PixelFormat = Some(desiredPixelFormat),
            FrameDataLocation = FrameDataLocation.Hardware
        };
}
