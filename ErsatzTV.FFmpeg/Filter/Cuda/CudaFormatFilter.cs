using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class CudaFormatFilter : BaseFilter
{
    private readonly IPixelFormat _pixelFormat;

    public CudaFormatFilter(IPixelFormat pixelFormat) => _pixelFormat = pixelFormat;

    public override string Filter => $"scale_cuda=format={_pixelFormat.FFmpegName}";

    public override FrameState NextState(FrameState currentState) =>
        currentState with { PixelFormat = Some(_pixelFormat) };
}
