using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class TonemapCudaFilter(FFmpegState ffmpegState, IPixelFormat desiredPixelFormat) : BaseFilter
{
    public override string Filter
    {
        get
        {
            // vulkan => cuda only works with 8-bit and 16-bit, not 10-bit
            string vulkanOutputFormat = desiredPixelFormat.FFmpegName;
            string cudaFormat = string.Empty;
            if (desiredPixelFormat.BitDepth == 10)
            {
                vulkanOutputFormat = "p016";
                cudaFormat = ",scale_cuda=format=p010";
            }

            return
                $"libplacebo=tonemapping={ffmpegState.TonemapAlgorithm}:colorspace=bt709:color_primaries=bt709:color_trc=bt709:format={vulkanOutputFormat},hwupload_cuda{cudaFormat}";
        }
    }

    public override FrameState NextState(FrameState currentState) =>
        currentState with
        {
            PixelFormat = Some(desiredPixelFormat),
            FrameDataLocation = FrameDataLocation.Hardware
        };
}
